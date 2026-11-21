using APIInverFlexSA.Application.Services;
using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas unitarias de las condiciones de activación de alertas de precio.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AlertEvaluationServiceTests
{
    private readonly AlertEvaluationService _service = new();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba la activación por porcentaje de subida.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Evaluate_PercentageRise_TriggersAtThreshold()
    {
        var alert = Alert(AlertCondition.PorcentajeSubida, 100m, 5m);

        var result = _service.Evaluate(alert, 105m);

        Assert.True(result);
        Assert.True(alert.IsTriggered);
        Assert.NotNull(alert.TriggeredAt);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una subida inferior al umbral no activa la alerta.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Evaluate_PercentageRise_BelowThresholdDoesNotTrigger()
    {
        var alert = Alert(AlertCondition.PorcentajeSubida, 100m, 5m);

        Assert.False(_service.Evaluate(alert, 104.99m));
        Assert.False(alert.IsTriggered);
        Assert.Null(alert.TriggeredAt);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba la activación por porcentaje de bajada.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Evaluate_PercentageDrop_TriggersAtThreshold()
    {
        var alert = Alert(AlertCondition.PorcentajeBajada, 100m, 10m);

        Assert.True(_service.Evaluate(alert, 90m));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba la activación cuando se alcanza un precio objetivo.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Evaluate_TargetPrice_TriggersAtOrAboveTarget()
    {
        var alert = Alert(AlertCondition.PrecioObjetivo, 100m, 120m);

        Assert.True(_service.Evaluate(alert, 120m));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una alerta ya activada no vuelve a activarse.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Evaluate_AlreadyTriggered_DoesNotReevaluate()
    {
        var alert = Alert(AlertCondition.PrecioObjetivo, 100m, 120m);
        Assert.True(_service.Evaluate(alert, 120m));
        var firstTriggeredAt = alert.TriggeredAt;

        Assert.False(_service.Evaluate(alert, 200m));
        Assert.Equal(firstTriggeredAt, alert.TriggeredAt);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea una alerta de precio para los escenarios de prueba.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static PriceAlert Alert(AlertCondition condition, decimal referencePrice, decimal threshold) => new()
    {
        Condition = condition, ReferencePrice = referencePrice, ThresholdValue = threshold
    };
}
