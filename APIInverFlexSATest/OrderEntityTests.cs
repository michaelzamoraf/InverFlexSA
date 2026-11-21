using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas de las transiciones de estado de la entidad de orden.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class OrderEntityTests
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba el cálculo del importe total.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void TotalAmount_IsQuantityTimesUnitPrice()
    {
        var order = new Order { Quantity = 3, UnitPrice = 12.5m };

        Assert.Equal(37.5m, order.TotalAmount);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una orden pendiente pasa a ejecutada al aprobarse.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Approve_PendingOrder_SetsExecutionData()
    {
        var advisorId = Guid.NewGuid();
        var order = new Order { Status = OrderStatus.PendienteAprobacion };

        order.Approve(advisorId);

        Assert.Equal(OrderStatus.Ejecutada, order.Status);
        Assert.Equal(advisorId, order.ApprovedByAdvisorId);
        Assert.NotNull(order.ExecutedAt);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una orden ejecutada no se puede aprobar.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Approve_ExecutedOrder_Throws()
    {
        var order = new Order { Status = OrderStatus.Ejecutada };

        Assert.Throws<InvalidOperationException>(() => order.Approve(Guid.NewGuid()));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba la cancelación de una orden pendiente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Cancel_PendingOrder_SetsCancelledStatus()
    {
        var order = new Order { Status = OrderStatus.PendienteAprobacion };

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelada, order.Status);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que una orden ejecutada no se puede cancelar.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public void Cancel_ExecutedOrder_Throws()
    {
        var order = new Order { Status = OrderStatus.Ejecutada };

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }
}
