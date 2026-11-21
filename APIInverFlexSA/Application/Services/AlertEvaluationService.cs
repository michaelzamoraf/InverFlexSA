using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Application.Services;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Implementa la evaluación de condiciones de alertas de precio.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AlertEvaluationService : IAlertEvaluationService
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Evalúa una alerta y registra el instante de activación una sola vez.
    /// </summary>
    /// <param name="alert">Alerta que se evaluará.</param>
    /// <param name="currentPrice">Precio actual.</param>
    /// <returns><see langword="true"/> cuando se cumple la condición.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public bool Evaluate(PriceAlert alert, decimal currentPrice)
    {
        ArgumentNullException.ThrowIfNull(alert);

        if (alert.IsTriggered)
            return false;

        var triggered = alert.Condition switch
        {
            AlertCondition.PorcentajeSubida => currentPrice >= alert.ReferencePrice * (1m + alert.ThresholdValue / 100m),
            AlertCondition.PorcentajeBajada => currentPrice <= alert.ReferencePrice * (1m - alert.ThresholdValue / 100m),
            AlertCondition.PrecioObjetivo => currentPrice >= alert.ThresholdValue,
            _ => throw new ArgumentOutOfRangeException(nameof(alert.Condition), alert.Condition, "Condición de alerta no soportada.")
        };

        if (triggered)
        {
            alert.IsTriggered = true;
            alert.TriggeredAt = DateTime.UtcNow;
        }

        return triggered;
    }
}
