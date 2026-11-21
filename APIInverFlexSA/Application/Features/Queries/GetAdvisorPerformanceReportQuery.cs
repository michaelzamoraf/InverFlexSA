using APIInverFlexSA.Application.DTOs;
using MediatR;

namespace APIInverFlexSA.Application.Features.Queries;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Solicita el reporte de rendimiento del conjunto de clientes asignados al asesor.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed record GetAdvisorPerformanceReportQuery(Guid AdvisorId, DateTime From, DateTime To)
    : IRequest<AdvisorPerformanceReportDto>;
