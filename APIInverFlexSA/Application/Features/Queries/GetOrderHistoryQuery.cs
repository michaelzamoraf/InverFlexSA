using APIInverFlexSA.Domain.Entities;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consulta paginada del historial de órdenes.
    /// </summary>
    /// <param name="ClientId">Cliente opcional por el que filtrar.</param>
    /// <param name="InstrumentId">Instrumento opcional.</param>
    /// <param name="Status">Estado opcional.</param>
    /// <param name="From">Fecha inicial inclusiva.</param>
    /// <param name="To">Fecha final inclusiva.</param>
    /// <param name="Page">Página solicitada.</param>
    /// <param name="PageSize">Número de elementos por página.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record GetOrderHistoryQuery(Guid? ClientId, Guid? InstrumentId, OrderStatus? Status, DateTime? From, DateTime? To, int Page = 1, int PageSize = 10) : IRequest<(IEnumerable<Order> Items, int TotalCount)>;
}
