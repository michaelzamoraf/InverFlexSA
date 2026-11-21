using APIInverFlexSA.Application.Features.Portfolios;
using APIInverFlexSA.Application.Exceptions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints de consulta de carteras de clientes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    [Authorize]
    public class PortfoliosController(IMediator mediator, IAuthorizationService authService) : ControllerBase
    {
        #region Atributos 

        private readonly IMediator _mediator = mediator;
        private readonly IAuthorizationService _authService = authService;

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
            //Valida mediante Policy personalizada si es dueño, asesor asignado o admin
            var authResult = await _authService.AuthorizeAsync(User, clientId, "CanAccessClientPortfolio");
            if (!authResult.Succeeded)
                return Forbid();

            try
            {
                var result = await _mediator.Send(new GetClientPortfolioQuery(clientId), ct);
                return Ok(result);
            }
            catch (MarketPriceUnavailableException ex)
            {
                return Problem(title: "Precio de mercado no disponible", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
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
            var authResult = await _authService.AuthorizeAsync(
                User, clientId, "CanAccessClientPortfolio");

            if (!authResult.Succeeded)
                return Forbid();

            Response.Headers.Append("Deprecated", "true");
            try
            {
                var result = await _mediator.Send(new GetClientPortfolioQuery(clientId), ct);
                return Ok(result);
            }
            catch (MarketPriceUnavailableException ex)
            {
                return Problem(title: "Precio de mercado no disponible", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        #endregion
    }
}
