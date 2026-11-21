using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Domain.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Repositorio de alertas de precio.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IPriceAlertRepository
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene las alertas activas.
        /// </summary>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Alertas activas.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<IEnumerable<PriceAlert>> GetActiveAlertsAsync(CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene las alertas pertenecientes a un cliente.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        Task<IEnumerable<PriceAlert>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Busca una alerta por identificador y propietario.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        Task<PriceAlert?> GetByIdForClientAsync(Guid alertId, Guid clientId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Marca la alerta y crea su notificación dentro de una misma transacción.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        Task<bool> TryMarkTriggeredAndAddNotificationAsync(
            Guid alertId, DateTime triggeredAt, PriceAlertNotification notification, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Elimina una alerta perteneciente al cliente.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        Task<bool> DeleteForClientAsync(Guid alertId, Guid clientId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Añade una alerta.
        /// </summary>
        /// <param name="alert">Alerta.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task AddAsync(PriceAlert alert, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Actualiza una alerta.
        /// </summary>
        /// <param name="alert">Alerta.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task UpdateAsync(PriceAlert alert, CancellationToken ct = default);
    }
}
