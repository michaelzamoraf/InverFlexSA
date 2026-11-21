namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Notificación de precio que puede consultar el cliente propietario.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record PriceAlertNotificationDto(
    Guid Id, Guid PriceAlertId, string Message, string Ticker,
    decimal TriggerPrice, DateTime CreatedAt, bool IsRead);
