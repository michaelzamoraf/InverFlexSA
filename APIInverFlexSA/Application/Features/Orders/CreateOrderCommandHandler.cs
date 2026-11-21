using APIInverFlexSA.Application.DTOs;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using MediatR;

namespace APIInverFlexSA.Application.Features.Orders
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea órdenes, calcula su importe y liquida las operaciones inmediatas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
    {
        #region Atributos

        private const decimal HighValueThreshold = 50000m;
        private readonly IOrderRepository _orderRepo;
        private readonly IInstrumentRepository _instrumentRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IMarketPriceService _priceService;
        private readonly IDistributedCacheService _cache;

        #endregion

        #region Constructor

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el handler con sus dependencias.
        /// </summary>
        /// <param name="orderRepo">Repositorio de órdenes.</param>
        /// <param name="instrumentRepo">Catálogo de instrumentos.</param>
        /// <param name="portfolioRepo">Repositorio de carteras.</param>
        /// <param name="priceService">Servicio de precios.</param>
        /// <param name="cache">Caché distribuido.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public CreateOrderCommandHandler(
            IOrderRepository orderRepo, IInstrumentRepository instrumentRepo,
            IPortfolioRepository portfolioRepo, IMarketPriceService priceService,
            IDistributedCacheService cache)
        {
            _orderRepo = orderRepo;
            _instrumentRepo = instrumentRepo;
            _portfolioRepo = portfolioRepo;
            _priceService = priceService;
            _cache = cache;
        }

        #endregion

        #region Manejador

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Crea una orden y la ejecuta o deja pendiente según su importe.
        /// </summary>
        /// <param name="request">Datos de la orden.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Orden creada.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
        {
            var instrument = await _instrumentRepo.GetByIdAsync(request.InstrumentId, ct)
                ?? throw new KeyNotFoundException("Instrumento no encontrado.");

            var currentPrice = await _priceService.GetCurrentPriceAsync(instrument.Ticker, ct);
            var totalAmount = request.Quantity * currentPrice;

            var requiresApproval = totalAmount > HighValueThreshold;

            var order = new Order
            {
                Id = Guid.NewGuid(),
                ClientId = request.ClientId,
                InstrumentId = instrument.Id,
                Type = request.Type,
                Quantity = request.Quantity,
                UnitPrice = currentPrice,
                Status = requiresApproval ? OrderStatus.PendienteAprobacion : OrderStatus.Ejecutada,
                ExecutedAt = requiresApproval ? null : DateTime.UtcNow
            };

            await _orderRepo.AddAsync(order, ct);

            if (!requiresApproval)
            {
                await ExecutePortfolioSettlementAsync(order, ct);
            }

            return new OrderDto(order.Id, order.ClientId, instrument.Ticker, order.Type,
                order.Quantity, order.UnitPrice, order.TotalAmount, order.Status, order.CreatedAt);
        }

        #endregion

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Ejecuta el liquidación de portafolio para una orden específica.
        /// </summary>
        /// <param name="order">Orden específica.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// -----------------------------------------------------------------------------------------------------------
        private async Task ExecutePortfolioSettlementAsync(Order order, CancellationToken ct)
        {
            var portfolio = await _portfolioRepo.GetByClientIdAsync(order.ClientId, ct)!;
            //Lógica de actualización de efectivo y posiciones...
            if (order.Type == OrderType.Compra)
            {
                if (portfolio!.AvailableCash < order.TotalAmount)
                    throw new InvalidOperationException("Saldo insuficiente.");
                portfolio.AvailableCash -= order.TotalAmount;
            }
            else
            {
                portfolio!.AvailableCash += order.TotalAmount;
            }

            await _portfolioRepo.UpdateAsync(portfolio, ct);
            //Invalidación explícita de caché distribuido en Redis
            await _cache.RemoveAsync($"portfolio:{order.ClientId}", ct);
        }
    }
}
