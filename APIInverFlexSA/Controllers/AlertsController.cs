using System.Security.Claims;
using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using APIInverFlexSA.Infrastructure.Persistence;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Controllers;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Administra alertas de precio y notificaciones del cliente autenticado.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(Roles = "cliente")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "cliente")]
public sealed class AlertsController(
    IPriceAlertRepository alerts,
    IMarketPriceService marketPrices,
    AppDbContext db,
    ILogger<AlertsController> logger) : ControllerBase
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea una alerta cuyo propietario se obtiene del JWT, nunca del body.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<PriceAlertDto>> Create([FromBody] CreatePriceAlertRequest request, CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();
        if (!Enum.IsDefined(request.Condition) || request.ThresholdValue <= 0 ||
            ((request.Condition == AlertCondition.PorcentajeSubida || request.Condition == AlertCondition.PorcentajeBajada)
                && request.ThresholdValue >= 100))
            return BadRequest(new { message = "El umbral debe ser positivo; para porcentajes debe ser menor que 100." });

        var instrument = await db.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive, ct);
        if (instrument is null) return NotFound(new { message = "El instrumento no existe o está inactivo." });

        decimal referencePrice;
        try
        {
            referencePrice = await marketPrices.GetCurrentPriceAsync(instrument.Ticker, ct);
            if (referencePrice <= 0) return Problem("El proveedor devolvió un precio inválido.", statusCode: 503);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo obtener el precio de referencia para {Ticker}.", instrument.Ticker);
            return Problem("No se pudo obtener un precio de referencia para crear la alerta.", statusCode: 503);
        }

        var alert = new PriceAlert
        {
            Id = Guid.NewGuid(), ClientId = clientId.Value, InstrumentId = instrument.Id,
            Condition = request.Condition, ReferencePrice = referencePrice,
            ThresholdValue = request.ThresholdValue, CreatedAt = DateTime.UtcNow
        };
        await alerts.AddAsync(alert, ct);
        return Created("/api/v1/Alerts", ToDto(alert, instrument.Ticker, instrument.Name));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Lista únicamente las alertas del cliente autenticado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetMine(CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();
        var items = await alerts.GetByClientIdAsync(clientId.Value, ct);
        return Ok(items.Select(x => ToDto(x, x.Instrument.Ticker, x.Instrument.Name)));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Elimina una alerta activa que pertenezca al cliente autenticado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();
        var deleted = await alerts.DeleteForClientAsync(id, clientId.Value, ct);
        return deleted ? NoContent() : NotFound(new { message = "No existe una alerta activa con ese identificador para el cliente." });
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consulta las notificaciones de alertas del cliente autenticado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpGet("notifications")]
    public async Task<ActionResult<IEnumerable<PriceAlertNotificationDto>>> GetNotifications(CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();
        var items = await db.PriceAlertNotifications.AsNoTracking()
            .Where(x => x.ClientId == clientId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PriceAlertNotificationDto(x.Id, x.PriceAlertId, x.Message,
                x.PriceAlert.Instrument.Ticker, x.TriggerPrice, x.CreatedAt, x.IsRead))
            .ToListAsync(ct);
        return Ok(items);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Marca como leída una notificación perteneciente al cliente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [HttpPatch("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();
        var affected = await db.PriceAlertNotifications
            .Where(x => x.Id == id && x.ClientId == clientId.Value)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);
        return affected == 1 ? NoContent() : NotFound();
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Permite obtener el identificador del usuario autenticado desde el JWT, usando "userId" o "sub" según la configuración de la API.
    /// </summary>
    /// <returns>El identificador del usuario autenticado, o null si no está autenticado.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    private Guid? GetUserId()
    {
        var value = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static PriceAlertDto ToDto(PriceAlert alert, string ticker, string name) =>
        new(alert.Id, alert.InstrumentId, ticker, name, alert.Condition.ToString(),
            alert.ReferencePrice, alert.ThresholdValue, alert.IsTriggered, alert.CreatedAt, alert.TriggeredAt);
}

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Datos necesarios para crear una alerta; no acepta ClientId desde el consumidor.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record CreatePriceAlertRequest(Guid InstrumentId, AlertCondition Condition, decimal ThresholdValue);
