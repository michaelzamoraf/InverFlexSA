using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Domain.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Repositorio del catálogo de instrumentos.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IInstrumentRepository
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Busca un instrumento por identificador.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Instrumento encontrado o <see langword="null"/>.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<Instrument?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene instrumentos paginados y filtrados.
        /// </summary>
        /// <param name="category">Categoría opcional.</param>
        /// <param name="sector">Sector opcional.</param>
        /// <param name="page">Página.</param>
        /// <param name="pageSize">Tamaño de página.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Elementos y total de resultados.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<(IEnumerable<Instrument> Items, int TotalCount)> GetPagedAsync(
            InstrumentCategory? category, string? sector, int page, int pageSize, CancellationToken ct = default);
    }
}
