using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIInverFlexSA.Infrastructure.Persistence;
using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints de administración de la plataforma.
    /// </summary>
    /// <param name="dbContext"></param>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    [Authorize(Roles = "admin")]
    [ApiExplorerSettings(GroupName = "admin")]
    public class AdminController(AppDbContext dbContext) : ControllerBase
    {
        #region Atributos

        private readonly AppDbContext _dbContext = dbContext;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Asigna un cliente a un asesor.
        /// </summary>
        /// <param name="request">Identificadores del asesor y del cliente.</param>
        /// <param name="ct">Token para cancelar la operación.</param>
        /// <returns>Confirmación de la asignación.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpPost("assign-client")]
        public async Task<IActionResult> AssignClientToAdvisor(
            [FromBody] AssignClientRequest request, CancellationToken ct)
        {
            if (request.AdvisorId == Guid.Empty || request.ClientId == Guid.Empty)
                return BadRequest(new { message = "Debe indicar identificadores válidos para asesor y cliente." });

            var advisor = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.AdvisorId, ct);
            if (advisor is null || advisor.Role != UserRole.Asesor)
                return BadRequest(new { message = "El usuario indicado no existe o no tiene el rol asesor." });

            var client = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.ClientId, ct);
            if (client is null || client.Role != UserRole.Cliente)
                return BadRequest(new { message = "El usuario indicado no existe o no tiene el rol cliente." });

            var alreadyAssigned = await _dbContext.AdvisorClients.AnyAsync(
                ac => ac.AdvisorId == request.AdvisorId && ac.ClientId == request.ClientId, ct);
            if (alreadyAssigned)
                return Conflict(new { message = "El cliente ya está asignado a ese asesor." });

            _dbContext.AdvisorClients.Add(new AdvisorClient
            {
                AdvisorId = request.AdvisorId,
                ClientId = request.ClientId,
                AssignedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(ct);
            return Created($"/api/v1/admin/assign-client", new
            {
                request.AdvisorId,
                request.ClientId,
                AssignedAt = DateTime.UtcNow
            });
        }

        #endregion

        #region Records

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Datos necesarios para crear una asignación asesor-cliente.
        /// </summary>
        /// <param name="AdvisorId">Asesor.</param>
        /// <param name="ClientId">Cliente.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public record AssignClientRequest(Guid AdvisorId, Guid ClientId);

        #endregion
    }
}
