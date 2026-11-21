using System.Collections.Concurrent;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Infrastructure.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Servicio de precios simulado. Genera una caminata aleatoria acotada alrededor del precio base
/// para que las alertas puedan dispararse durante las demostraciones.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class MockMarketPriceService(AppDbContext db) : IMarketPriceService
{
    private static readonly ConcurrentDictionary<string, decimal> LastPrices = new(StringComparer.OrdinalIgnoreCase);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene una cotización simulada: cada consulta modifica el último precio hasta +/-1% y
    /// mantiene el precio dentro del rango de 50%-150% del precio base.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<decimal> GetCurrentPriceAsync(string ticker, CancellationToken ct = default)
    {
        var instrument = await db.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ticker == ticker && x.IsActive, ct)
            ?? throw new KeyNotFoundException($"No existe el instrumento {ticker}.");

        var minimum = instrument.BasePrice * 0.50m;
        var maximum = instrument.BasePrice * 1.50m;
        var next = LastPrices.AddOrUpdate(
            instrument.Ticker,
            instrument.BasePrice,
            (_, previous) =>
            {
                var variation = (decimal)(Random.Shared.NextDouble() * 0.02 - 0.01);
                return Math.Clamp(previous * (1m + variation), minimum, maximum);
            });

        return Math.Round(next, 4);
    }
}
