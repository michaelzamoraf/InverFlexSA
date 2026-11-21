using APIInverFlexSA.Application.Features.Queries;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas del reporte JSON del asesor y su aislamiento por asignación.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AdvisorPerformanceReportQueryHandlerTests
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el reporte de desempeño del asesor devuelve datos estructurados y consistentes para carteras asignadas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Handle_ReturnsStructuredPerformanceForAssignedPortfolios()
    {
        var advisorId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var instrument = new Instrument { Id = Guid.NewGuid(), Ticker = "AAPL", Name = "Apple", IsActive = true };
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(), ClientId = clientId,
            Client = new User { Id = clientId, FullName = "Cliente de prueba" },
            AvailableCash = 100m,
            Positions = new List<PortfolioPosition>
            {
                new() { Id = Guid.NewGuid(), InstrumentId = instrument.Id, Instrument = instrument,
                    Quantity = 2m, AverageBuyPrice = 100m }
            }
        };

        var portfolioRepository = new Mock<IPortfolioRepository>();
        portfolioRepository.Setup(x => x.GetByAdvisorIdAsync(advisorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { portfolio });
        var priceService = new Mock<IMarketPriceService>();
        priceService.Setup(x => x.GetCurrentPriceAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(110m);
        var cache = new Mock<IDistributedCacheService>();
        cache.Setup(x => x.GetAsync<decimal?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GetAdvisorPerformanceReportQueryHandler(
            portfolioRepository.Object, priceService.Object, cache.Object,
            NullLogger<GetAdvisorPerformanceReportQueryHandler>.Instance);
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 11, 20);

        var report = await handler.Handle(new GetAdvisorPerformanceReportQuery(advisorId, from, to), CancellationToken.None);

        Assert.Equal(advisorId, report.AdvisorId);
        Assert.Equal(1, report.TotalClients);
        Assert.True(report.IsCurrentSnapshot);
        Assert.Equal(320m, report.TotalPortfolioValue); // 2 * 110 + 100 cash
        Assert.Equal(300m, report.TotalInvested); // 2 * 100 + 100 cash
        Assert.Equal(20m, report.TotalReturn);
        Assert.Single(report.Clients);
        Assert.Equal(clientId, report.Clients.Single().ClientId);
        portfolioRepository.Verify(x => x.GetByAdvisorIdAsync(advisorId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
