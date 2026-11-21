namespace APIInverFlexSA.Application.DTOs;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Reporte consolidado de rendimiento de los clientes asignados al asesor.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record AdvisorPerformanceReportDto(
    Guid ReportId, Guid AdvisorId, DateTime GeneratedAtUtc, DateTime From, DateTime To,
    string Currency, int TotalClients, decimal TotalInvested, decimal TotalPortfolioValue,
    decimal TotalReturn, decimal ReturnPercentage, bool IsCurrentSnapshot,
    IReadOnlyCollection<AdvisorReportItemDto> Clients);
