using System.Text.Json;
using APIInverFlexSA.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace APIInverFlexSA.Infrastructure.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Implementa el almacenamiento distribuido mediante Redis y serialización JSON.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class RedisDistributedCacheService(IDistributedCache cache) : IDistributedCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>Obtiene y deserializa un valor almacenado en la caché.</summary>
    /// <typeparam name="T">Tipo del valor almacenado.</typeparam>
    /// <param name="key">Clave del valor.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>El valor deserializado o <see langword="default"/> si no existe.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(key, ct);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>Serializa y almacena un valor durante el periodo indicado.</summary>
    /// <typeparam name="T">Tipo del valor que se almacenará.</typeparam>
    /// <param name="key">Clave del valor.</param>
    /// <param name="value">Valor que se almacenará.</param>
    /// <param name="expiration">Tiempo de expiración relativa.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default) =>
        cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }, ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>Elimina un valor de la caché.</summary>
    /// <param name="key">Clave del valor que se eliminará.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);
}
