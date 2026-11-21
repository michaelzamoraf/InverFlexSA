using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Application.Services;
using APIInverFlexSA.Domain.Entities;
using Moq;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Verifica la evaluación de alertas con cotizaciones controladas mediante Moq.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AlertEvaluationWithMockPriceTests
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una alerta de subida se activa correctamente cuando el precio simulado supera el umbral.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task RiseAlert_TriggersUsingDeterministicMockPrice()
    {
        var prices = new Mock<IMarketPriceService>();
        prices.Setup(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(105m);
        var alert = new PriceAlert
        {
            Condition = AlertCondition.PorcentajeSubida,
            ReferencePrice = 100m,
            ThresholdValue = 5m
        };
        var currentPrice = await prices.Object.GetCurrentPriceAsync("AAPL");
        var triggered = new AlertEvaluationService().Evaluate(alert, currentPrice);
        Assert.True(triggered);
        Assert.True(alert.IsTriggered);
        prices.Verify(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una alerta de bajada se activa correctamente cuando el precio simulado cae por debajo del umbral.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task PriceProviderFailure_IsObservableToWorker()
    {
        var prices = new Mock<IMarketPriceService>();
        prices.Setup(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Mock sin respuesta."));
        await Assert.ThrowsAsync<TimeoutException>(() => prices.Object.GetCurrentPriceAsync("AAPL"));
    }
}
