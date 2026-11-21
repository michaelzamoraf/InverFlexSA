using APIInverFlexSA.Application.Features.Orders;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using Moq;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas unitarias de creación, valoración y liquidación de órdenes.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IInstrumentRepository> _instruments = new();
    private readonly Mock<IPortfolioRepository> _portfolios = new();
    private readonly Mock<IMarketPriceService> _prices = new();
    private readonly Mock<IDistributedCacheService> _cache = new();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una orden de importe elevado queda pendiente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_HighValueOrder_RequiresApproval()
    {
        var instrument = Instrument();
        _instruments.Setup(x => x.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>())).ReturnsAsync(instrument);
        _prices.Setup(x => x.GetCurrentPriceAsync(instrument.Ticker, It.IsAny<CancellationToken>())).ReturnsAsync(100m);

        var result = await Handler().Handle(new(Guid.NewGuid(), instrument.Id, OrderType.Compra, 501), CancellationToken.None);

        Assert.Equal(OrderStatus.PendienteAprobacion, result.Status);
        _portfolios.Verify(x => x.UpdateAsync(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una compra de importe normal se liquida y descuenta efectivo.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_NormalPurchase_SettlesAndInvalidatesCache()
    {
        var clientId = Guid.NewGuid();
        var instrument = Instrument();
        var portfolio = new Portfolio { ClientId = clientId, AvailableCash = 1000m };
        _instruments.Setup(x => x.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>())).ReturnsAsync(instrument);
        _prices.Setup(x => x.GetCurrentPriceAsync(instrument.Ticker, It.IsAny<CancellationToken>())).ReturnsAsync(100m);
        _portfolios.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(portfolio);

        await Handler().Handle(new(clientId, instrument.Id, OrderType.Compra, 2), CancellationToken.None);

        Assert.Equal(800m, portfolio.AvailableCash);
        _cache.Verify(x => x.RemoveAsync($"portfolio:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una venta incrementa el efectivo de la cartera.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_Sale_AddsSettlementAmount()
    {
        var clientId = Guid.NewGuid();
        var instrument = Instrument();
        var portfolio = new Portfolio { ClientId = clientId, AvailableCash = 100m };
        _instruments.Setup(x => x.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>())).ReturnsAsync(instrument);
        _prices.Setup(x => x.GetCurrentPriceAsync(instrument.Ticker, It.IsAny<CancellationToken>())).ReturnsAsync(25m);
        _portfolios.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(portfolio);

        await Handler().Handle(new(clientId, instrument.Id, OrderType.Venta, 2), CancellationToken.None);

        Assert.Equal(150m, portfolio.AvailableCash);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que no se puede liquidar una compra sin saldo.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_InsufficientCash_Throws()
    {
        var clientId = Guid.NewGuid();
        var instrument = Instrument();
        _instruments.Setup(x => x.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>())).ReturnsAsync(instrument);
        _prices.Setup(x => x.GetCurrentPriceAsync(instrument.Ticker, It.IsAny<CancellationToken>())).ReturnsAsync(100m);
        _portfolios.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(new Portfolio { ClientId = clientId, AvailableCash = 1m });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler().Handle(new(clientId, instrument.Id, OrderType.Compra, 2), CancellationToken.None));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un instrumento inexistente no permite crear la orden.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_MissingInstrument_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _instruments.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Instrument?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Handler().Handle(new(Guid.NewGuid(), id, OrderType.Compra, 1), CancellationToken.None));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea el handler usando los dobles de prueba configurados.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private CreateOrderCommandHandler Handler() => new(_orders.Object, _instruments.Object, _portfolios.Object, _prices.Object, _cache.Object);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea un instrumento financiero para los escenarios de prueba.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static Instrument Instrument() => new() { Id = Guid.NewGuid(), Ticker = "TEST", Name = "Test", BasePrice = 100m };
}
