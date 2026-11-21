using APIInverFlexSA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace APIInverFlexSA.Infrastructure.Persistence
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Contexto de Entity Framework Core para la base de datos de InverFlex.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class AppDbContext : DbContext, IAppDbContext
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el contexto con sus opciones.
        /// </summary>
        /// <param name="options">Opciones de Entity Framework Core.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Usuarios.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<User> Users => Set<User>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Asignaciones asesor-cliente.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<AdvisorClient> AdvisorClients => Set<AdvisorClient>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Instrumentos.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<Instrument> Instruments => Set<Instrument>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Carteras.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Posiciones.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Órdenes.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<Order> Orders => Set<Order>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Alertas de precio.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

        /// <summary>Notificaciones generadas por alertas de precio.</summary>
        public DbSet<PriceAlertNotification> PriceAlertNotifications => Set<PriceAlertNotification>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Configura relaciones, restricciones y precisión de los datos financieros.
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo.</param>
        /// -----------------------------------------------------------------------------------------------------------
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuración de Relación Asesor-Cliente (Tabla intermedia)
            modelBuilder.Entity<AdvisorClient>(entity =>
            {
                entity.HasKey(ac => new { ac.AdvisorId, ac.ClientId });

                entity.HasOne(ac => ac.Advisor)
                    .WithMany(u => u.AssignedClients)
                    .HasForeignKey(ac => ac.AdvisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ac => ac.Client)
                    .WithMany(u => u.AssignedAdvisors)
                    .HasForeignKey(ac => ac.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Configuración de Portfolio y Posiciones
            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasOne(p => p.Client)
                    .WithOne(u => u.Portfolio)
                    .HasForeignKey<Portfolio>(p => p.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PortfolioPosition>(entity =>
            {
                entity.HasKey(pp => pp.Id);
                entity.HasOne(pp => pp.Portfolio)
                    .WithMany(p => p.Positions)
                    .HasForeignKey(pp => pp.PortfolioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pp => pp.Instrument)
                    .WithMany()
                    .HasForeignKey(pp => pp.InstrumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                //Precisión decimal para finanzas
                entity.Property(pp => pp.Quantity).HasColumnType("decimal(18,4)");
                entity.Property(pp => pp.AverageBuyPrice).HasColumnType("decimal(18,4)");
            });

            //Configuración de Instrumentos
            modelBuilder.Entity<Instrument>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.HasIndex(i => i.Ticker).IsUnique();
                entity.Property(i => i.BasePrice).HasColumnType("decimal(18,4)");
            });

            //Configuración de Órdenes y Concurrencia Optimista en SQLite
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Quantity).HasColumnType("decimal(18,4)");
                entity.Property(o => o.UnitPrice).HasColumnType("decimal(18,4)");

                //En SQLite, simulamos RowVersion mediante un campo de tipo BLOB o Rowversion/Byte[] gestionado por EF Core
                entity.Property(o => o.RowVersion)
                      .IsRowVersion();
            });

            //Configuración de Alertas de Precio
            modelBuilder.Entity<PriceAlert>(entity =>
            {
                entity.HasKey(pa => pa.Id);
                entity.Property(pa => pa.ReferencePrice).HasColumnType("decimal(18,4)");
                entity.Property(pa => pa.ThresholdValue).HasColumnType("decimal(18,4)");
                entity.HasIndex(pa => new { pa.ClientId, pa.IsTriggered });
                entity.HasOne(pa => pa.Instrument).WithMany().HasForeignKey(pa => pa.InstrumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PriceAlertNotification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Message).HasMaxLength(500);
                entity.Property(n => n.TriggerPrice).HasColumnType("decimal(18,4)");
                entity.HasIndex(n => new { n.ClientId, n.CreatedAt });
                entity.HasIndex(n => n.PriceAlertId).IsUnique();
                entity.HasOne(n => n.PriceAlert).WithMany().HasForeignKey(n => n.PriceAlertId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
