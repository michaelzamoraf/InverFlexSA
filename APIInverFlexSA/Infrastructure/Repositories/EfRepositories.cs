using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Domain.Interfaces;
using APIInverFlexSA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Infrastructure.Repositories;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Repositorio Entity Framework para las relaciones entre asesores y clientes.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AdvisorClientRepository(AppDbContext db) : IAdvisorClientRepository
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba si un cliente está asignado al asesor indicado.
    /// </summary>
    /// <param name="advisorId">Identificador del asesor.</param>
    /// <param name="clientId">Identificador del cliente.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si existe la asignación; en caso contrario, <see langword="false"/>.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public Task<bool> IsClientAssignedToAdvisorAsync(Guid advisorId, Guid clientId, CancellationToken ct = default) =>
        db.AdvisorClients.AnyAsync(x => x.AdvisorId == advisorId && x.ClientId == clientId, ct);
}

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Repositorio Entity Framework para instrumentos financieros.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class InstrumentRepository(AppDbContext db) : IInstrumentRepository
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene un instrumento activo por su identificador.
    /// </summary>
    /// <param name="id">Identificador del instrumento.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>El instrumento encontrado o <see langword="null"/> si no existe.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public Task<Instrument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Instruments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene una página de instrumentos activos y el total de resultados.
    /// </summary>
    /// <param name="category">Categoría opcional para filtrar.</param>
    /// <param name="sector">Sector opcional para filtrar.</param>
    /// <param name="page">Número de página, comenzando en uno.</param>
    /// <param name="pageSize">Cantidad máxima de elementos por página.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Los instrumentos de la página y el total de instrumentos coincidentes.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<(IEnumerable<Instrument> Items, int TotalCount)> GetPagedAsync(
        InstrumentCategory? category, string? sector, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Instruments.AsNoTracking().Where(x => x.IsActive);
        if (category.HasValue) query = query.Where(x => x.Category == category.Value);
        if (!string.IsNullOrWhiteSpace(sector)) query = query.Where(x => x.Sector.Contains(sector));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Ticker).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Repositorio Entity Framework para órdenes de inversión.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene una orden junto con su instrumento.
    /// </summary>
    /// <param name="orderId">Identificador de la orden.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>La orden encontrada o <see langword="null"/> si no existe.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default) =>
        db.Orders.Include(x => x.Instrument).FirstOrDefaultAsync(x => x.Id == orderId, ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene una página de órdenes aplicando los filtros indicados.
    /// </summary>
    /// <param name="clientId">Cliente opcional para filtrar.</param>
    /// <param name="instrumentId">Instrumento opcional para filtrar.</param>
    /// <param name="status">Estado opcional para filtrar.</param>
    /// <param name="from">Fecha mínima de creación.</param>
    /// <param name="to">Fecha máxima de creación.</param>
    /// <param name="page">Número de página, comenzando en uno.</param>
    /// <param name="pageSize">Cantidad máxima de elementos por página.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Las órdenes de la página y el total de órdenes coincidentes.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        Guid? clientId, Guid? instrumentId, OrderStatus? status, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking().Include(x => x.Instrument).AsQueryable();
        if (clientId.HasValue) query = query.Where(x => x.ClientId == clientId.Value);
        if (instrumentId.HasValue) query = query.Where(x => x.InstrumentId == instrumentId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Agrega una orden y guarda los cambios.
    /// </summary>
    /// <param name="order">Orden que se agregará.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await db.Orders.AddAsync(order, ct);
        await db.SaveChangesAsync(ct);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Actualiza una orden y guarda los cambios.
    /// </summary>
    /// <param name="order">Orden con los datos actualizados.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Update(order);
        await db.SaveChangesAsync(ct);
    }
}

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Repositorio Entity Framework para carteras de inversión.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class PortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene la cartera de un cliente con sus posiciones e instrumentos.
    /// </summary>
    /// <param name="clientId">Identificador del cliente.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>La cartera encontrada o <see langword="null"/> si no existe.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public Task<Portfolio?> GetByClientIdAsync(Guid clientId, CancellationToken ct = default) =>
        db.Portfolios.Include(x => x.Positions).ThenInclude(x => x.Instrument)
            .FirstOrDefaultAsync(x => x.ClientId == clientId, ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene las carteras de los clientes asignados a un asesor.
    /// </summary>
    /// <param name="advisorId">Identificador del asesor.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Las carteras de los clientes asignados.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<IEnumerable<Portfolio>> GetByAdvisorIdAsync(Guid advisorId, CancellationToken ct = default) =>
        await db.Portfolios.Include(x => x.Client).Include(x => x.Positions).ThenInclude(x => x.Instrument)
            .Where(p => db.AdvisorClients.Any(a => a.AdvisorId == advisorId && a.ClientId == p.ClientId))
            .AsNoTracking().ToListAsync(ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Actualiza una cartera y guarda los cambios.
    /// </summary>
    /// <param name="portfolio">Cartera con los datos actualizados.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task UpdateAsync(Portfolio portfolio, CancellationToken ct = default)
    {
        db.Portfolios.Update(portfolio);
        await db.SaveChangesAsync(ct);
    }
}

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Repositorio Entity Framework para alertas de precio.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class PriceAlertRepository(AppDbContext db) : IPriceAlertRepository
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene las alertas de precio que aún no se han activado.
    /// </summary>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Las alertas activas junto con sus instrumentos.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<IEnumerable<PriceAlert>> GetActiveAlertsAsync(CancellationToken ct = default) =>
        await db.PriceAlerts.Include(x => x.Instrument).Where(x => !x.IsTriggered).AsNoTracking().ToListAsync(ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene las alertas de un cliente sin exponer las de otros usuarios.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<IEnumerable<PriceAlert>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default) =>
        await db.PriceAlerts.Include(x => x.Instrument).Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.CreatedAt).AsNoTracking().ToListAsync(ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene una alerta únicamente si pertenece al cliente indicado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public Task<PriceAlert?> GetByIdForClientAsync(Guid alertId, Guid clientId, CancellationToken ct = default) =>
        db.PriceAlerts.Include(x => x.Instrument)
            .FirstOrDefaultAsync(x => x.Id == alertId && x.ClientId == clientId, ct);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Marca la alerta y crea su notificación de forma atómica para evitar activaciones duplicadas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<bool> TryMarkTriggeredAndAddNotificationAsync(
        Guid alertId, DateTime triggeredAt, PriceAlertNotification notification, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var affected = await db.PriceAlerts.Where(x => x.Id == alertId && !x.IsTriggered)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsTriggered, true)
                .SetProperty(x => x.TriggeredAt, triggeredAt), ct);
        if (affected != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await db.PriceAlertNotifications.AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Elimina una alerta solo cuando pertenece al cliente indicado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task<bool> DeleteForClientAsync(Guid alertId, Guid clientId, CancellationToken ct = default)
    {
        var affected = await db.PriceAlerts.Where(x => x.Id == alertId && x.ClientId == clientId && !x.IsTriggered)
            .ExecuteDeleteAsync(ct);
        return affected == 1;
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Agrega una alerta de precio y guarda los cambios.
    /// </summary>
    /// <param name="alert">Alerta que se agregará.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task AddAsync(PriceAlert alert, CancellationToken ct = default)
    {
        await db.PriceAlerts.AddAsync(alert, ct);
        await db.SaveChangesAsync(ct);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Actualiza una alerta de precio y guarda los cambios.
    /// </summary>
    /// <param name="alert">Alerta con los datos actualizados.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task UpdateAsync(PriceAlert alert, CancellationToken ct = default)
    {
        db.PriceAlerts.Update(alert);
        await db.SaveChangesAsync(ct);
    }
}
