using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Domain.Entities;
using MediatR;

namespace APIInverFlexSA.Application.Features.Orders
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Solicita la creación de una orden para un cliente.
    /// </summary>
    /// <param name="ClientId">Cliente titular.</param>
    /// <param name="InstrumentId">Instrumento negociado.</param>
    /// <param name="Type">Compra o venta.</param>
    /// <param name="Quantity">Cantidad solicitada.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record CreateOrderCommand(Guid ClientId, Guid InstrumentId, OrderType Type, decimal Quantity) : IRequest<OrderDto>;
}
