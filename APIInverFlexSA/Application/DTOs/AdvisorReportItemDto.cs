namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Elemento del informe de rentabilidad consolidado de un cliente.
/// </summary>
/// <param name="ClientId">Identificador del cliente.</param>
/// <param name="ClientName">Nombre completo del cliente.</param>
/// <param name="TotalValue">Valor actual de mercado más efectivo disponible.</param>
/// <param name="TotalInvested">Costo de adquisición más efectivo disponible.</param>
/// <param name="ReturnAmount">Ganancia o pérdida no realizada.</param>
/// <param name="ReturnPercentage">Rentabilidad porcentual calculada.</param>
/// -----------------------------------------------------------------------------------------------------------
public record AdvisorReportItemDto(Guid ClientId, string ClientName, decimal TotalValue,
    decimal TotalInvested, decimal ReturnAmount, decimal ReturnPercentage);
