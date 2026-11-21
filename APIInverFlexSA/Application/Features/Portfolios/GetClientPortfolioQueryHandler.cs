using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Application.Exceptions;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Portfolios
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene y valora las posiciones de la cartera de un cliente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class GetClientPortfolioQueryHandler : IRequestHandler<GetClientPortfolioQuery, PortfolioDto>
    {
        #region Atributos

        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IMarketPriceService _marketPriceService;
        private readonly IDistributedCacheService _cache;
        private readonly ILogger<GetClientPortfolioQueryHandler> _logger;
        private static readonly TimeSpan PriceTtl = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan StalePriceTtl = TimeSpan.FromHours(24);

        #endregion

        #region Constructor

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el handler.
        /// </summary>
        /// <param name="portfolioRepo">Repositorio de carteras.</param>
        /// <param name="marketPriceService">Servicio de precios.</param>
        /// <param name="cache">Caché distribuido.</param>
        /// <param name="logger">Logger.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public GetClientPortfolioQueryHandler(
            IPortfolioRepository portfolioRepo,
            IMarketPriceService marketPriceService,
            IDistributedCacheService cache,
            ILogger<GetClientPortfolioQueryHandler> logger)
        {
            _portfolioRepo = portfolioRepo;
            _marketPriceService = marketPriceService;
            _cache = cache;
            _logger = logger;
        }

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Calcula el valor de mercado y la rentabilidad de la cartera.
        /// </summary>
        /// <param name="request">Cliente consultado.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Resumen valorado.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        public async Task<PortfolioDto> Handle(GetClientPortfolioQuery request, CancellationToken ct)
        {
            var cacheKey = $"portfolio:{request.ClientId}";
            try
            {
                var cachedPortfolio = await _cache.GetAsync<PortfolioDto>(cacheKey, ct);
                if (cachedPortfolio is not null) return cachedPortfolio;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogWarning(ex, "Redis no disponible al consultar el portafolio {ClientId}.", request.ClientId); }

            var portfolio = await _portfolioRepo.GetByClientIdAsync(request.ClientId, ct)
                ?? throw new KeyNotFoundException("Portafolio no encontrado.");

            var positionsDto = new List<PositionDto>();
            decimal totalMarketValue = 0;
            decimal totalCostBasis = 0;

            foreach (var pos in portfolio.Positions)
            {
                var ticker = pos.Instrument.Ticker;
                var priceCacheKey = $"price:latest:{ticker}";
                decimal? currentPrice = null;
                try { currentPrice = await _cache.GetAsync<decimal?>(priceCacheKey, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { _logger.LogWarning(ex, "No se pudo leer el precio vigente de {Ticker} desde Redis.", ticker); }

                if (!currentPrice.HasValue)
                {
                    try
                    {
                        currentPrice = await _marketPriceService.GetCurrentPriceAsync(ticker, ct);
                        if (currentPrice <= 0) throw new InvalidOperationException($"El proveedor devolvió un precio inválido para {ticker}.");
                        try
                        {
                            await _cache.SetAsync(priceCacheKey, currentPrice.Value, PriceTtl, ct);
                            await _cache.SetAsync($"price:stale:{ticker}", currentPrice.Value, StalePriceTtl, ct);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        { _logger.LogWarning(ex, "No se pudo actualizar la caché de precios para {Ticker}.", ticker); }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception priceException)
                    {
                        _logger.LogError(priceException, "Falló el servicio de precios para {Ticker}; se intentará el último precio conocido.", ticker);
                        try { currentPrice = await _cache.GetAsync<decimal?>($"price:stale:{ticker}", ct); }
                        catch (Exception cacheException) when (cacheException is not OperationCanceledException)
                        { _logger.LogError(cacheException, "No se pudo recuperar el precio de respaldo de {Ticker}.", ticker); }
                        if (!currentPrice.HasValue || currentPrice <= 0)
                            throw new MarketPriceUnavailableException($"No hay un precio vigente ni un precio de respaldo válido para {ticker}. No se puede valorar el portafolio.", priceException);
                    }
                }

                var currentValue = pos.Quantity * currentPrice.Value;
                var costBasis = pos.Quantity * pos.AverageBuyPrice;

                totalMarketValue += currentValue;
                totalCostBasis += costBasis;

                positionsDto.Add(new PositionDto(
                    pos.Instrument.Ticker,
                    pos.Instrument.Name,
                    pos.Quantity,
                    pos.AverageBuyPrice,
                    currentPrice.Value,
                    currentValue,
                    currentValue - costBasis
                ));
            }

            var returnPct = totalCostBasis > 0 ? ((totalMarketValue - totalCostBasis) / totalCostBasis) * 100 : 0;

            var result = new PortfolioDto(
                portfolio.ClientId,
                portfolio.AvailableCash,
                totalCostBasis,
                totalMarketValue + portfolio.AvailableCash,
                Math.Round(returnPct, 2),
                positionsDto
            );

            try { await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogWarning(ex, "No se pudo almacenar el portafolio {ClientId} en Redis.", request.ClientId); }
            return result;
        }

        #endregion
    }
}
