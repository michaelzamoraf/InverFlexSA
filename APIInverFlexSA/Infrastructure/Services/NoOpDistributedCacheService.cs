using APIInverFlexSA.Application.Interfaces;

namespace APIInverFlexSA.Infrastructure.Services;

/// <summary>
/// Implementación nula de caché para las etapas que todavía no incorporan Redis.
/// Las lecturas siempre se consideran no almacenadas y las escrituras no realizan operaciones.
/// </summary>
public sealed class NoOpDistributedCacheService : IDistributedCacheService
{
    /// <summary>Devuelve el valor predeterminado porque no existe almacenamiento de caché.</summary>
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult<T?>(default);

    /// <summary>No almacena el valor recibido.</summary>
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <summary>No realiza operaciones porque no existe almacenamiento de caché.</summary>
    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        Task.CompletedTask;
}
