using APIInverFlexSA.Application.Features.Queries;
using APIInverFlexSA.Domain.Entities;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints de consulta del catálogo de instrumentos.
    /// </summary>
    /// <param name="mediator"></param>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    [Authorize]
    public class InstrumentsController(IMediator mediator) : ControllerBase
    {
        #region Atributos

        private readonly IMediator _mediator = mediator;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene instrumentos filtrados y paginados.
        /// </summary>
        /// <param name="category">Categoría opcional.</param>
        /// <param name="sector">Sector opcional.</param>
        /// <param name="page">Página, empezando en uno.</param>
        /// <param name="pageSize">Elementos por página.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Resultado paginado del catálogo.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpGet]
        [ApiExplorerSettings(GroupName = "cliente")]
        public async Task<IActionResult> GetInstruments(
            [FromQuery] InstrumentCategory? category, [FromQuery] string? sector,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _mediator.Send(new GetInstrumentsQuery(category, sector, page, pageSize), ct);
            return Ok(new { Total = total, Page = page, PageSize = pageSize, Data = items });
        }

        #endregion
    }
}
