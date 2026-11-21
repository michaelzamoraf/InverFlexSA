using APIInverFlexSA.Application.Features.Portfolios;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints de consulta de carteras de clientes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class PortfoliosController(IMediator mediator) : ControllerBase
    {
        #region Atributos 

        private readonly IMediator _mediator = mediator;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene la cartera de un cliente autorizado.
        /// </summary>
        /// <param name="clientId">Cliente consultado.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Cartera valorada.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpGet("{clientId:guid}")]
        [ApiExplorerSettings(GroupName = "cliente")]
        public async Task<IActionResult> GetPortfolio(Guid clientId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetClientPortfolioQuery(clientId), ct);
            return Ok(result);
        }

        //Endpoint de ejemplo marcado para depreciación futura
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene la versión heredada del recurso de cartera.
        /// </summary>
        /// <param name="clientId">Cliente consultado.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Cartera valorada.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpGet("legacy/{clientId:guid}")]
        [ApiExplorerSettings(GroupName = "cliente")]
        public async Task<IActionResult> GetLegacyPortfolio(Guid clientId, CancellationToken ct)
        {
            Response.Headers.Append("Deprecated", "true");
            var result = await _mediator.Send(new GetClientPortfolioQuery(clientId), ct);
            return Ok(result);
        }

        #endregion
    }
}
