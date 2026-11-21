using APIInverFlexSA.Application.Features.Orders;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using Moq;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas unitarias de la aprobación de órdenes y sus reglas de autorización.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class ApproveOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IAdvisorClientRepository> _assignments = new();
    private readonly Mock<IPortfolioRepository> _portfolios = new();
    private readonly Mock<IDistributedCacheService> _cache = new();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Verifica que un asesor asignado puede aprobar una orden pendiente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_AssignedAdvisor_ApprovesAndInvalidatesCache()
    {
        var clientId = Guid.NewGuid();
        var advisorId = Guid.NewGuid();
        var order = PendingOrder(clientId);
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _assignments.Setup(x => x.IsClientAssignedToAdvisorAsync(advisorId, clientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Handler().Handle(new(order.Id, advisorId), CancellationToken.None);

        Assert.Equal(OrderStatus.Ejecutada, order.Status);
        Assert.Equal(advisorId, order.ApprovedByAdvisorId);
        _orders.Verify(x => x.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(x => x.RemoveAsync($"portfolio:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Verifica que un administrador puede aprobar sin asignación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_Admin_BypassesAssignmentCheck()
    {
        var order = PendingOrder(Guid.NewGuid());
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await Handler().Handle(new(order.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal(OrderStatus.Ejecutada, order.Status);
        _assignments.Verify(x => x.IsClientAssignedToAdvisorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Verifica que una orden inexistente produce una excepción de dominio de aplicación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_MissingOrder_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _orders.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Handler().Handle(new(id, Guid.NewGuid()), CancellationToken.None));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Verifica que un asesor no asignado no puede aprobar la orden.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_UnassignedAdvisor_ThrowsUnauthorized()
    {
        var order = PendingOrder(Guid.NewGuid());
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _assignments.Setup(x => x.IsClientAssignedToAdvisorAsync(It.IsAny<Guid>(), order.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Handler().Handle(new(order.Id, Guid.NewGuid()), CancellationToken.None));
        _orders.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Verifica que no se puede aprobar una orden ya ejecutada.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_AlreadyExecutedOrder_ThrowsInvalidOperation()
    {
        var order = PendingOrder(Guid.NewGuid());
        order.Status = OrderStatus.Ejecutada;
        _orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _assignments.Setup(x => x.IsClientAssignedToAdvisorAsync(It.IsAny<Guid>(), order.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler().Handle(new(order.Id, Guid.NewGuid()), CancellationToken.None));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea el handler usando los dobles de prueba configurados.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private ApproveOrderCommandHandler Handler() => new(_orders.Object, _assignments.Object, _portfolios.Object, _cache.Object);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea una orden pendiente de aprobación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static Order PendingOrder(Guid clientId) => new()
    {
        Id = Guid.NewGuid(), ClientId = clientId, Status = OrderStatus.PendienteAprobacion,
        Quantity = 1, UnitPrice = 100
    };
}
