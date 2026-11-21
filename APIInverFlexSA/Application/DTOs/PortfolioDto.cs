namespace APIInverFlexSA.Application.DTOs
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Resumen valorado de la cartera de un cliente.
    /// </summary>
    /// <param name="ClientId">Identificador del cliente.</param><param name="AvailableCash">Efectivo disponible.</param>
    /// <param name="TotalInvestedValue">Coste total de las posiciones.</param><param name="TotalPortfolioValue">Valor total incluida la liquidez.</param>
    /// <param name="ReturnPercentage">Rentabilidad porcentual.</param><param name="Positions">Posiciones valoradas.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record PortfolioDto(Guid ClientId, decimal AvailableCash, decimal TotalInvestedValue, decimal TotalPortfolioValue, decimal ReturnPercentage, List<PositionDto> Positions);
}
