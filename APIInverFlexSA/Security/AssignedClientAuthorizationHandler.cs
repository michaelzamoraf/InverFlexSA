using APIInverFlexSA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Security
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Autoriza el acceso a carteras según rol y asignación asesor-cliente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class AssignedClientAuthorizationHandler(AppDbContext dbContext) : AuthorizationHandler<AssignedClientRequirement, Guid>
    {
        #region Atributos

        private readonly AppDbContext _dbContext = dbContext;

        #endregion

        #region Métodos Protegidos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Evalúa si el usuario puede acceder al cliente objetivo.
        /// </summary>
        /// <param name="context">Contexto de autorización.</param>
        /// <param name="requirement">Requisito evaluado.</param>
        /// <param name="targetClientId">Cliente objetivo.</param>
        /// -----------------------------------------------------------------------------------------------------------
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AssignedClientRequirement requirement,
            Guid targetClientId)
        {
            //Extraer el rol y el ID del usuario desde los claims del JWT (compatible con ClaimTypes y claims personalizados)
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value
                           ?? context.User.FindFirst("role")?.Value;

            var userIdStr = context.User.FindFirst("userId")?.Value
                            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userRole) || !Guid.TryParse(userIdStr, out var currentUserId))
            {
                context.Fail();
                return;
            }

            //El Administrador tiene acceso total
            if (userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return;
            }

            //El Cliente solo puede acceder a su propio portafolio
            if (userRole.Equals("cliente", StringComparison.OrdinalIgnoreCase) && currentUserId == targetClientId)
            {
                context.Succeed(requirement);
                return;
            }

            //El Asesor solo accede si el cliente está formalmente asignado en la Base de Datos
            if (userRole.Equals("asesor", StringComparison.OrdinalIgnoreCase))
            {
                var isAssigned = await _dbContext.AdvisorClients
                    .AnyAsync(ac => ac.AdvisorId == currentUserId && ac.ClientId == targetClientId);

                if (isAssigned)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }

        #endregion
    }
}
