namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Sobre de caché serializable del catálogo paginado.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record InstrumentCatalogCacheDto(List<InstrumentCatalogItemDto> Items, int TotalCount);
