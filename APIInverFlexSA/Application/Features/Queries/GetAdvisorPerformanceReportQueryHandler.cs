using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Application.Exceptions;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Calcula un reporte consolidado sobre las carteras actuales de los clientes asignados.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class GetAdvisorPerformanceReportQueryHandler(
    IPortfolioRepository portfolios, IMarketPriceService prices, IDistributedCacheService cache,
    ILogger<GetAdvisorPerformanceReportQueryHandler> logger)
    : IRequestHandler<GetAdvisorPerformanceReportQuery, AdvisorPerformanceReportDto>
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Valora las posiciones y calcula rendimiento no realizado respecto al costo promedio registrado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<AdvisorPerformanceReportDto> Handle(GetAdvisorPerformanceReportQuery request, CancellationToken ct)
    {
        var assignedPortfolios = await portfolios.GetByAdvisorIdAsync(request.AdvisorId, ct);
        var rows = new List<AdvisorReportItemDto>();
        foreach (var portfolio in assignedPortfolios)
        {
            decimal invested = 0m;
            decimal marketValue = 0m;
            foreach (var position in portfolio.Positions)
            {
                var ticker = position.Instrument.Ticker;
                decimal current = 0m;
                try
                {
                    current = await cache.GetAsync<decimal?>( $"price:latest:{ticker}", ct) ?? 0m;
                }
                catch (Exception cacheException) when (cacheException is not OperationCanceledException)
                {
                    logger.LogWarning(cacheException, "No se pudo leer el precio vigente de {Ticker} desde Redis.", ticker);
                }

                if (current <= 0)
                {
                    try
                    {
                        current = await prices.GetCurrentPriceAsync(ticker, ct);
                        if (current <= 0) throw new InvalidOperationException($"Precio inválido para {ticker}.");
                        try
                        {
                            await cache.SetAsync($"price:latest:{ticker}", current, TimeSpan.FromSeconds(60), ct);
                            await cache.SetAsync($"price:stale:{ticker}", current, TimeSpan.FromHours(24), ct);
                        }
                        catch (Exception cacheException) when (cacheException is not OperationCanceledException)
                        { logger.LogWarning(cacheException, "No se pudo actualizar la caché del precio {Ticker}.", ticker); }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception priceException)
                    {
                        logger.LogWarning(priceException, "Falló el proveedor para {Ticker}; se consultará el último precio conocido.", ticker);
                        try { current = await cache.GetAsync<decimal?>( $"price:stale:{ticker}", ct) ?? 0m; }
                        catch (Exception cacheException) when (cacheException is not OperationCanceledException)
                        { logger.LogError(cacheException, "No se pudo recuperar el precio de respaldo de {Ticker}.", ticker); }
                        if (current <= 0)
                            throw new MarketPriceUnavailableException($"No existe precio válido ni respaldo para {ticker}; no se puede generar el reporte.", priceException);
                    }
                }
                invested += position.Quantity * position.AverageBuyPrice;
                marketValue += position.Quantity * current;
            }
            var totalInvestedIncludingCash = invested + portfolio.AvailableCash;
            var totalValueIncludingCash = marketValue + portfolio.AvailableCash;
            var returnAmount = totalValueIncludingCash - totalInvestedIncludingCash;
            var returnPct = totalInvestedIncludingCash > 0 ? returnAmount / totalInvestedIncludingCash * 100m : 0m;
            var clientName = portfolio.Client?.FullName ?? "Cliente";
            rows.Add(new AdvisorReportItemDto(portfolio.ClientId, clientName,
                Math.Round(totalValueIncludingCash, 2), Math.Round(totalInvestedIncludingCash, 2),
                Math.Round(returnAmount, 2), Math.Round(returnPct, 2)));
        }
        var totalInvested = rows.Sum(x => x.TotalInvested);
        var totalValue = rows.Sum(x => x.TotalValue);
        var totalReturn = totalValue - totalInvested;
        var totalReturnPct = totalInvested > 0 ? totalReturn / totalInvested * 100m : 0m;
        logger.LogInformation("Reporte de asesor {AdvisorId}: {ClientCount} clientes asignados.", request.AdvisorId, rows.Count);
        return new AdvisorPerformanceReportDto(Guid.NewGuid(), request.AdvisorId, DateTime.UtcNow,
            request.From.Date, request.To.Date, "USD", rows.Count, Math.Round(totalInvested, 2),
            Math.Round(totalValue, 2), Math.Round(totalReturn, 2), Math.Round(totalReturnPct, 2),
            true, rows);
    }
}
