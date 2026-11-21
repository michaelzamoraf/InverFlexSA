using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Ejecuta la consulta paginada del historial de órdenes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, (IEnumerable<Order> Items, int TotalCount)>
    {
        #region Atributos

        private readonly IOrderRepository _repo;

        #endregion

        #region Constructor

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el handler.
        /// </summary>
        /// <param name="repo">Repositorio de órdenes.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public GetOrderHistoryQueryHandler(IOrderRepository repo) => _repo = repo;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Devuelve las órdenes filtradas.
        /// </summary>
        /// <param name="request">Filtros y paginación.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Órdenes y total.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        public Task<(IEnumerable<Order> Items, int TotalCount)> Handle(GetOrderHistoryQuery request, CancellationToken ct)
            => _repo.GetPagedAsync(request.ClientId, request.InstrumentId, request.Status, request.From, request.To, request.Page, request.PageSize, ct);

        #endregion
    }
}
