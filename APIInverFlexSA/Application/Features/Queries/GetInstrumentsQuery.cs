using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Domain.Entities;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Consulta paginada y filtrable del catálogo de instrumentos.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record GetInstrumentsQuery(
    InstrumentCategory? Category, string? Sector, int Page = 1, int PageSize = 10)
    : IRequest<(IEnumerable<InstrumentCatalogItemDto> Items, int TotalCount)>;
