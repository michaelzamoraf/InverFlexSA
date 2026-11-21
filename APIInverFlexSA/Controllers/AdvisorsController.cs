using System.Security.Claims;
using APIInverFlexSA.Application.Features.Queries;
using APIInverFlexSA.Application.Exceptions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIInverFlexSA.Controllers;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Endpoints de consulta y reportes para asesores financieros.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(Roles = "asesor,admin")]
[ApiExplorerSettings(GroupName = "asesor")]
public sealed class AdvisorsController(IMediator mediator) : ControllerBase
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Devuelve un JSON consolidado de rendimiento de los clientes asignados al asesor autenticado.
    /// </summary>
    /// <remarks>El período se refleja en el reporte; las entidades actuales no conservan snapshots históricos. El cálculo es una valoración actual y no una rentabilidad histórica exacta.</remarks>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpGet("reports/performance")]
    [Produces("application/json")]
    public async Task<IActionResult> GetClientsPerformanceReport(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from == default || to == default || from.Date > to.Date)
            return BadRequest(new { message = "Indique un período válido: from debe ser menor o igual que to." });
        var advisorClaim = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(advisorClaim, out var advisorId)) return Unauthorized();
        try
        {
            var report = await mediator.Send(new GetAdvisorPerformanceReportQuery(advisorId, from, to), ct);
            return Ok(report);
        }
        catch (MarketPriceUnavailableException ex)
        {
            return Problem(title: "Precio de mercado no disponible", detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
