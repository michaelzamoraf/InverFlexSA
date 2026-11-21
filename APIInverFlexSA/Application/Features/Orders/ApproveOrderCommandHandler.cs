using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Orders
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Procesa la aprobación de órdenes y actualiza la cartera relacionada.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class ApproveOrderCommandHandler : IRequestHandler<ApproveOrderCommand, Unit>
    {
        #region Atributos

        private readonly IOrderRepository _orderRepo;
        private readonly IAdvisorClientRepository _advisorClientRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IDistributedCacheService _cache;

        #endregion

        #region Constructor

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el handler con sus repositorios y caché.
        /// </summary>
        /// <param name="orderRepo">Repositorio de órdenes.</param>
        /// <param name="advisorClientRepo">Repositorio de asignaciones.</param>
        /// <param name="portfolioRepo">Repositorio de carteras.</param>
        /// <param name="cache">Caché distribuido.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public ApproveOrderCommandHandler(
            IOrderRepository orderRepo, IAdvisorClientRepository advisorClientRepo,
            IPortfolioRepository portfolioRepo, IDistributedCacheService cache)
        {
            _orderRepo = orderRepo;
            _advisorClientRepo = advisorClientRepo;
            _portfolioRepo = portfolioRepo;
            _cache = cache;
        }

        #endregion

        #region Manejador

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Aprueba una orden pendiente si el asesor está asignado al cliente.
        /// </summary>
        /// <param name="request">Comando de aprobación.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Resultado vacío de MediatR.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        public async Task<Unit> Handle(ApproveOrderCommand request, CancellationToken ct)
        {
            var order = await _orderRepo.GetByIdAsync(request.OrderId, ct)
                ?? throw new KeyNotFoundException("Orden no encontrada.");

            if (!request.IsAdmin)
            {
                var isAssigned = await _advisorClientRepo.IsClientAssignedToAdvisorAsync(
                    request.AdvisorId, order.ClientId, ct);
                if (!isAssigned)
                    throw new UnauthorizedAccessException("El asesor no tiene asignado a este cliente.");
            }

            //Cambia estado (valida que esté pendiente)
            order.Approve(request.AdvisorId);

            //Si otro asesor aprobó simultáneamente, EF Core lanzará DbUpdateConcurrencyException por RowVersion
            await _orderRepo.UpdateAsync(order, ct);

            //Invalidar caché del portafolio tras ejecución
            await _cache.RemoveAsync($"portfolio:{order.ClientId}", ct);

            return Unit.Value;
        }

        #endregion
    }
}
