namespace APIInverFlexSA.Application.DTOs
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Posición de un instrumento con su valoración actual.
    /// </summary>
    /// <param name="Ticker">Ticker del instrumento.</param><param name="Name">Nombre del instrumento.</param>
    /// <param name="Quantity">Cantidad mantenida.</param><param name="AverageBuyPrice">Precio medio de compra.</param>
    /// <param name="CurrentPrice">Precio actual.</param><param name="CurrentValue">Valor de mercado.</param>
    /// <param name="UnrealizedPnL">Pérdida o ganancia no realizada.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record PositionDto(string Ticker, string Name, decimal Quantity, decimal AverageBuyPrice, decimal CurrentPrice, decimal CurrentValue, decimal UnrealizedPnL);
}
