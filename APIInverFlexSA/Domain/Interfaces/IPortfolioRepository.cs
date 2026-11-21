using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Domain.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Repositorio de carteras de clientes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IPortfolioRepository
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene la cartera de un cliente.
        /// </summary>
        /// <param name="clientId">Cliente.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Cartera o <see langword="null"/>.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<Portfolio?> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene las carteras asociadas a un asesor.
        /// </summary>
        /// <param name="advisorId">Asesor.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Carteras.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<IEnumerable<Portfolio>> GetByAdvisorIdAsync(Guid advisorId, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Actualiza una cartera.
        /// </summary>
        /// <param name="portfolio">Cartera.</param>
        /// <param name="ct">Cancelación.</param>
        /// -----------------------------------------------------------------------------------------------------------
        Task UpdateAsync(Portfolio portfolio, CancellationToken ct = default);
    }
}
