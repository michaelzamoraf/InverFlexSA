using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Infrastructure.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Servicio de precios simulado; genera una variación acotada sobre el precio base.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class MockMarketPriceService(AppDbContext db) : IMarketPriceService
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene un precio simulado para un instrumento activo.
    /// </summary>
    /// <param name="ticker">Ticker del instrumento.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Precio base con una variación aleatoria acotada.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<decimal> GetCurrentPriceAsync(string ticker, CancellationToken ct = default)
    {
        var instrument = await db.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ticker == ticker && x.IsActive, ct)
            ?? throw new KeyNotFoundException($"No existe el instrumento {ticker}.");

        //Variación demostrativa de hasta +/- 2%; no representa cotizaciones reales.
        var variation = (decimal)(Random.Shared.NextDouble() * 0.04 - 0.02);
        return Math.Round(instrument.BasePrice * (1m + variation), 4);
    }
}
