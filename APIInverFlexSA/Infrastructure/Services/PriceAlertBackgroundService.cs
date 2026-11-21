using APIInverFlexSA.Application.Features.Alerts;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using APIInverFlexSA.Domain.Entities;
using MediatR;

namespace APIInverFlexSA.Infrastructure.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Evalúa periódicamente alertas activas y publica un evento por activación.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class PriceAlertBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PriceAlertBackgroundService> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        Math.Clamp(configuration.GetValue<int?>("Alerts:PollingSeconds") ?? 30, 5, 3600));

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Ejecuta ciclos independientes para evitar que un fallo detenga el worker.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessCycleAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Error durante el ciclo de evaluación de alertas."); }

            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Permite la evaluación de alertas en un ciclo aislado para que un fallo no detenga el worker.
    /// </summary>
    /// <param name="ct">Token de cancelación</param>
    /// -----------------------------------------------------------------------------------------------------------
    private async Task ProcessCycleAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPriceAlertRepository>();
        var prices = scope.ServiceProvider.GetRequiredService<IMarketPriceService>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IAlertEvaluationService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var activeAlerts = await repository.GetActiveAlertsAsync(ct);

        foreach (var alert in activeAlerts)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var price = await prices.GetCurrentPriceAsync(alert.Instrument.Ticker, ct);
                if (price <= 0) { logger.LogWarning("Precio no válido para {Ticker}; se omite alerta {AlertId}.", alert.Instrument.Ticker, alert.Id); continue; }
                if (!evaluator.Evaluate(alert, price)) continue;

                //La activación y la notificación quedan persistidas en una sola transacción.
                //La actualización condicional evita que dos instancias disparen la misma alerta.
                var triggeredAt = alert.TriggeredAt ?? DateTime.UtcNow;
                var message = $"La alerta de {alert.Instrument.Ticker} se activó al alcanzar {price:F4}.";
                var notification = new PriceAlertNotification
                {
                    Id = Guid.NewGuid(), ClientId = alert.ClientId, PriceAlertId = alert.Id,
                    Message = message, TriggerPrice = price, CreatedAt = triggeredAt
                };
                if (!await repository.TryMarkTriggeredAndAddNotificationAsync(alert.Id, triggeredAt, notification, ct)) continue;
                //El evento se publica después de persistir; la notificación in-app ya es durable.
                await publisher.Publish(new PriceAlertTriggeredEvent(
                    alert.Id, alert.ClientId, alert.Instrument.Ticker, price, message), ct);
                logger.LogInformation("Alerta {AlertId} activada para cliente {ClientId}.", alert.Id, alert.ClientId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo evaluar la alerta {AlertId} ({Ticker}).", alert.Id, alert.Instrument.Ticker);
            }
        }
    }
}
