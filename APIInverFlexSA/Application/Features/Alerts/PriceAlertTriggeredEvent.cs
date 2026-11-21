using MediatR;

namespace APIInverFlexSA.Application.Features.Alerts;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Evento publicado cuando una alerta de precio se activa.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record PriceAlertTriggeredEvent(
    Guid AlertId, Guid ClientId, string Ticker, decimal TriggerPrice, string Message) : INotification;
