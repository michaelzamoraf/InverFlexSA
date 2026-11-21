namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Representación de una alerta de precio para la API.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record PriceAlertDto(
    Guid Id, Guid InstrumentId, string Ticker, string InstrumentName,
    string Condition, decimal ReferencePrice, decimal ThresholdValue,
    bool IsTriggered, DateTime CreatedAt, DateTime? TriggeredAt);
