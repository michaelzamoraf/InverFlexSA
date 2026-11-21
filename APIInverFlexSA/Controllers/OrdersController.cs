using APIInverFlexSA.Application.Features.Orders;
using APIInverFlexSA.Application.Features.Queries;
using APIInverFlexSA.Domain.Entities;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints para crear, aprobar y consultar órdenes.
    /// </summary>
    /// <param name="mediator"></param>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    [Authorize]
    public class OrdersController(IMediator mediator) : ControllerBase
    {
        #region Atributos

        private readonly IMediator _mediator = mediator;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Crea una orden para el cliente autenticado.
        /// </summary>
        /// <param name="request">Instrumento, tipo y cantidad.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Orden creada.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "cliente")]
        [ApiExplorerSettings(GroupName = "cliente")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            var clientId = Guid.Parse(User.FindFirstValue("userId")!);
            var result = await _mediator.Send(new CreateOrderCommand(clientId, request.InstrumentId, request.Type, request.Quantity), ct);
            return Created("/api/v1/orders/history", result);
        }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Aprueba una orden pendiente de importe elevado.
        /// </summary>
        /// <param name="orderId">Orden a aprobar.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Respuesta sin contenido.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpPost("{orderId:guid}/approve")]
        [Authorize(Roles = "asesor,admin")]
        [ApiExplorerSettings(GroupName = "asesor")]
        public async Task<IActionResult> ApproveHighValueOrder(Guid orderId, CancellationToken ct)
        {
            if (!Guid.TryParse(User.FindFirstValue("userId"), out var actorId))
                return Forbid();

            var isAdmin = User.IsInRole("admin");
            await _mediator.Send(new ApproveOrderCommand(orderId, actorId, isAdmin), ct);
            return NoContent();
        }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene el historial de órdenes visible para el usuario.
        /// </summary>
        /// <param name="instrumentId">Filtro de instrumento.</param>
        /// <param name="status">Filtro de estado.</param>
        /// <param name="from">Fecha inicial.</param>
        /// <param name="to">Fecha final.</param>
        /// <param name="page">Página.</param>
        /// <param name="pageSize">Tamaño.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Historial paginado.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpGet("history")]
        [Authorize(Roles = "cliente")]
        [ApiExplorerSettings(GroupName = "cliente")]
        public async Task<IActionResult> GetOrderHistory(
            [FromQuery] Guid? instrumentId, [FromQuery] OrderStatus? status,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var userIdValue = User.FindFirstValue("userId");
            if (!Guid.TryParse(userIdValue, out var clientId))
                return Forbid();

            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "page debe ser mayor que cero y pageSize debe estar entre 1 y 100." });

            if (from.HasValue && to.HasValue && from.Value > to.Value)
                return BadRequest(new { message = "La fecha inicial no puede ser posterior a la fecha final." });

            var (items, total) = await _mediator.Send(
                new GetOrderHistoryQuery(clientId, instrumentId, status, from, to, page, pageSize), ct);

            return Ok(new { Total = total, Page = page, PageSize = pageSize, Data = items });
        }

        #endregion

        #region Records

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Datos recibidos para crear una orden.
        /// </summary>
        /// <param name="InstrumentId">Instrumento.</param>
        /// <param name="Type">Tipo de operación.</param>
        /// <param name="Quantity">Cantidad.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public record CreateOrderRequest(Guid InstrumentId, OrderType Type, decimal Quantity);

        #endregion
    }
}
