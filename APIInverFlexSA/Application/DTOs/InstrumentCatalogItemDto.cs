namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Instrumento del catálogo con precio base y última cotización simulada conocida.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record InstrumentCatalogItemDto(
    Guid Id, string Ticker, string Name, string Category, string Sector,
    decimal BasePrice, decimal CurrentPrice, bool IsPriceStale, bool IsActive);
