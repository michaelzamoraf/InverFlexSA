using APIInverFlexSA.Application.DTOs;
using MediatR;

namespace APIInverFlexSA.Application.Features.Portfolios
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consulta la cartera valorada de un cliente.
    /// </summary>
    /// <param name="ClientId">Cliente cuya cartera se consulta.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record GetClientPortfolioQuery(Guid ClientId) : IRequest<PortfolioDto>;
}
