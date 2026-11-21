namespace APIInverFlexSA.Application.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Abstracción del caché distribuido usado por la aplicación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IDistributedCacheService
    {
        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene y deserializa un valor almacenado.
        /// </summary>
        /// <typeparam name="T">Tipo del valor.</typeparam>
        /// <param name="key">Clave.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Valor o <see langword="null"/> si no existe.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Almacena un valor durante el periodo indicado.
        /// </summary>
        /// <typeparam name="T">Tipo del valor.</typeparam>
        /// <param name="key">Clave.</param>
        /// <param name="value">Valor.</param>
        /// <param name="expiration">Duración.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Elimina una entrada del caché.
        /// </summary>
        /// <param name="key">Clave a eliminar.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task RemoveAsync(string key, CancellationToken ct = default);

        #endregion
    }
}
