using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Domain.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Repositorio de órdenes de inversión.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IOrderRepository
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene una orden por identificador.
        /// </summary>
        /// <param name="orderId">Identificador.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Orden o <see langword="null"/>.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene el historial filtrado y paginado.
        /// </summary>
        /// <param name="clientId">Cliente opcional.</param>
        /// <param name="instrumentId">Instrumento opcional.</param>
        /// <param name="status">Estado opcional.</param>
        /// <param name="from">Fecha inicial.</param>
        /// <param name="to">Fecha final.</param>
        /// <param name="page">Página.</param>
        /// <param name="pageSize">Tamaño.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Órdenes y total.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
            Guid? clientId, Guid? instrumentId, OrderStatus? status,
            DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Añade una orden al repositorio.
        /// </summary>
        /// <param name="order">Orden.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task AddAsync(Order order, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Actualiza una orden existente.
        /// </summary>
        /// <param name="order">Orden.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task UpdateAsync(Order order, CancellationToken ct = default);
    }
}
