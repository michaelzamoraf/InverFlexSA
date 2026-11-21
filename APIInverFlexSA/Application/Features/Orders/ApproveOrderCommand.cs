using MediatR;

namespace APIInverFlexSA.Application.Features.Orders
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Solicita la aprobación de una orden de importe elevado.
    /// </summary>
    /// <param name="OrderId">Orden que se desea aprobar.</param>
    /// <param name="AdvisorId">Asesor que realiza la aprobación.</param>
    /// <param name="IsAdmin">Indica si el asesor es un administrador.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record ApproveOrderCommand(Guid OrderId, Guid AdvisorId, bool IsAdmin = false) : IRequest<Unit>;
}
