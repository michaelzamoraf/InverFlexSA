using Microsoft.AspNetCore.Authorization;

namespace APIInverFlexSA.Security
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Requisito de autorización para acceder a la cartera de un cliente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class AssignedClientRequirement : IAuthorizationRequirement
    {
    }
}
