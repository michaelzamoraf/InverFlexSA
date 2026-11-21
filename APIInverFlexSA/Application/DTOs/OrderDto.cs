using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Application.DTOs
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Representa una orden expuesta por la capa de aplicación.
    /// </summary>
    /// <param name="Id">Identificador de la orden.</param><param name="ClientId">Cliente propietario.</param>
    /// <param name="Ticker">Ticker del instrumento.</param><param name="Type">Tipo de operación.</param>
    /// <param name="Quantity">Cantidad negociada.</param><param name="UnitPrice">Precio unitario.</param>
    /// <param name="TotalAmount">Importe total de la orden.</param><param name="Status">Estado actual.</param>
    /// <param name="CreatedAt">Fecha de creación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record OrderDto(Guid Id, Guid ClientId, string Ticker, OrderType Type, decimal Quantity, decimal UnitPrice, decimal TotalAmount, OrderStatus Status, DateTime CreatedAt);
}
