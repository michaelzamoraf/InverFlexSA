using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Application.Exceptions;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Consulta el catálogo y sus cotizaciones, con caché Redis de 60 segundos.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class GetInstrumentsQueryHandler : IRequestHandler<GetInstrumentsQuery, (IEnumerable<InstrumentCatalogItemDto> Items, int TotalCount)>
{
    private static readonly TimeSpan PriceTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StalePriceTtl = TimeSpan.FromHours(24);
    private readonly IInstrumentRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly IMarketPriceService _prices;
    private readonly ILogger<GetInstrumentsQueryHandler> _logger;

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inicializa el handler con repositorio, proveedor de precios y caché distribuida.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public GetInstrumentsQueryHandler(IInstrumentRepository repository, IDistributedCacheService cache,
        IMarketPriceService prices, ILogger<GetInstrumentsQueryHandler> logger)
    { _repository = repository; _cache = cache; _prices = prices; _logger = logger; }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Devuelve catálogo paginado; los filtros forman parte de la clave Redis.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<(IEnumerable<InstrumentCatalogItemDto> Items, int TotalCount)> Handle(GetInstrumentsQuery request, CancellationToken ct)
    {
        var key = $"catalog:instruments:{request.Category?.ToString() ?? "all"}:{request.Sector?.Trim().ToUpperInvariant() ?? "all"}:{request.Page}:{request.PageSize}";
        try
        {
            var cached = await _cache.GetAsync<InstrumentCatalogCacheDto>(key, ct);
            if (cached is not null) return (cached.Items, cached.TotalCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "No se pudo leer el catálogo desde Redis; se consultará la base de datos."); }

        var result = await _repository.GetPagedAsync(request.Category, request.Sector, request.Page, request.PageSize, ct);
        var items = new List<InstrumentCatalogItemDto>();
        foreach (var instrument in result.Items)
        {
            var (price, stale) = await GetPriceAsync(instrument.Ticker, instrument.BasePrice, ct);
            items.Add(new InstrumentCatalogItemDto(instrument.Id, instrument.Ticker, instrument.Name,
                instrument.Category.ToString(), instrument.Sector, instrument.BasePrice, price, stale, instrument.IsActive));
        }

        var entry = new InstrumentCatalogCacheDto(items, result.TotalCount);
        try { await _cache.SetAsync(key, entry, PriceTtl, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "No se pudo almacenar el catálogo en Redis."); }
        return (items, result.TotalCount);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Permite obtener la cotización actual de un instrumento, con respaldo de caché y manejo de errores.
    /// </summary>
    /// <param name="ticker">El símbolo del instrumento.</param>
    /// <param name="basePrice">El precio base del instrumento.</param>
    /// <param name="ct">El token de cancelación.</param>
    /// <returns>Una tupla con el precio y un indicador de si es stale.</returns>
    /// <exception cref="InvalidOperationException">Excepción que se lanza cuando el precio devuelto por el proveedor es inválido.</exception>
    /// <exception cref="MarketPriceUnavailableException">Excepción que se lanza cuando no hay cotización disponible ni de respaldo.</exception>
    /// -----------------------------------------------------------------------------------------------------------
    private async Task<(decimal Price, bool IsStale)> GetPriceAsync(string ticker, decimal basePrice, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<decimal?>($"price:latest:{ticker}", ct);
            if (cached.HasValue && cached.Value > 0) return (cached.Value, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "No se pudo leer el precio vigente de {Ticker} desde Redis.", ticker); }

        try
        {
            var current = await _prices.GetCurrentPriceAsync(ticker, ct);
            if (current <= 0) throw new InvalidOperationException($"El proveedor devolvió un precio inválido para {ticker}.");
            try
            {
                await _cache.SetAsync($"price:latest:{ticker}", current, PriceTtl, ct);
                await _cache.SetAsync($"price:stale:{ticker}", current, StalePriceTtl, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogWarning(ex, "No se pudo almacenar la cotización de {Ticker}.", ticker); }
            return (current, false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception providerException)
        {
            try
            {
                var stale = await _cache.GetAsync<decimal?>($"price:stale:{ticker}", ct);
                if (stale.HasValue && stale.Value > 0) return (stale.Value, true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "No se pudo recuperar la cotización de respaldo de {Ticker}.", ticker); }

            //BasePrice es una referencia estática de catálogo, no se presenta como cotización actual.
            throw new MarketPriceUnavailableException(
                $"No existe cotización actual ni respaldo para {ticker}; el precio base ({basePrice}) no se utilizará como precio de mercado.", providerException);
        }
    }

}
