using APIInverFlexSA.Application.Features.Alerts;
using MediatR;

namespace APIInverFlexSA.Infrastructure.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Observa el evento posterior a la persistencia de la notificación in-app.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class PriceAlertNotificationHandler(ILogger<PriceAlertNotificationHandler> logger)
    : INotificationHandler<PriceAlertTriggeredEvent>
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Registra el evento para telemetría; los canales externos pueden suscribirse al mismo evento.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public Task Handle(PriceAlertTriggeredEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Evento PriceAlertTriggered publicado para alerta {AlertId}, cliente {ClientId}, ticker {Ticker}, precio {Price}.",
            notification.AlertId, notification.ClientId, notification.Ticker, notification.TriggerPrice);
        return Task.CompletedTask;
    }
}
