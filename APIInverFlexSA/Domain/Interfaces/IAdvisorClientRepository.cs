namespace APIInverFlexSA.Domain.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consulta las relaciones entre asesores y clientes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IAdvisorClientRepository
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Indica si el cliente está asignado al asesor.
        /// </summary>
        /// <param name="advisorId">Asesor.</param>
        /// <param name="clientId">Cliente.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns><see langword="true"/> si existe la asignación.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<bool> IsClientAssignedToAdvisorAsync(Guid advisorId, Guid clientId, CancellationToken ct = default);
    }
}
