using APIInverFlexSA.Application.Interfaces;
using Moq;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas deterministas de consumidores del contrato de precios mediante Moq.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class MarketPriceServiceContractTests
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el servicio de precios simulado devuelve el valor configurado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task MockedMarketPriceService_ReturnsConfiguredPrice()
    {
        var service = new Mock<IMarketPriceService>();
        service.Setup(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(105m);
        var price = await service.Object.GetCurrentPriceAsync("AAPL");
        Assert.Equal(105m, price);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el servicio de precios simulado puede lanzar una excepción para simular un fallo del proveedor.
    /// </summary>
    /// <returns></returns>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task MockedMarketPriceService_CanSimulateProviderFailure()
    {
        var service = new Mock<IMarketPriceService>();
        service.Setup(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Proveedor de precios no disponible."));
        await Assert.ThrowsAsync<TimeoutException>(() => service.Object.GetCurrentPriceAsync("AAPL"));
    }
}
