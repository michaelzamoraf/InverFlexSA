using APIInverFlexSA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace APIInverFlexSA.Infrastructure.Persistence
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inicializa la base de datos con datos de demostración.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public static class DbInitializer
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Crea el esquema y carga usuarios, relaciones e instrumentos si está vacío.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public static async Task SeedAsync(AppDbContext context)
        {
            //Asegurar que la base de datos esté creada
            await context.Database.EnsureCreatedAsync();

            // EnsureCreated no actualiza esquemas SQLite existentes. Esta sentencia aditiva
            // garantiza la tabla de notificaciones para bases locales creadas por versiones previas.
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "PriceAlertNotifications" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_PriceAlertNotifications" PRIMARY KEY,
                    "ClientId" TEXT NOT NULL,
                    "PriceAlertId" TEXT NOT NULL,
                    "Message" TEXT NOT NULL,
                    "TriggerPrice" decimal(18,4) NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "IsRead" INTEGER NOT NULL,
                    CONSTRAINT "FK_PriceAlertNotifications_PriceAlerts_PriceAlertId"
                        FOREIGN KEY ("PriceAlertId") REFERENCES "PriceAlerts" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_PriceAlertNotifications_ClientId_CreatedAt"
                    ON "PriceAlertNotifications" ("ClientId", "CreatedAt");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PriceAlertNotifications_PriceAlertId"
                    ON "PriceAlertNotifications" ("PriceAlertId");
                CREATE INDEX IF NOT EXISTS "IX_PriceAlerts_ClientId_IsTriggered"
                    ON "PriceAlerts" ("ClientId", "IsTriggered");
                """);

            //Si la base ya existe, actualizar hashes heredados del seeder anterior.
            //Esto permite reutilizar el archivo SQLite incluido en el proyecto.
            var passwordHasher = new PasswordHasher<User>();
            var demoPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin@inverflex.com"] = "Admin123!",
                ["asesor1@inverflex.com"] = "Asesor123!",
                ["asesor2@inverflex.com"] = "Asesor123!",
                ["cliente1@inverflex.com"] = "Cliente123!",
                ["cliente2@inverflex.com"] = "Cliente123!",
                ["cliente3@inverflex.com"] = "Cliente123!",
                ["cliente4@inverflex.com"] = "Cliente123!",
                ["cliente5@inverflex.com"] = "Cliente123!"
            };

            var existingUsers = await context.Users.ToListAsync();
            foreach (var existingUser in existingUsers)
            {
                if (demoPasswords.TryGetValue(existingUser.Email, out var password) &&
                    (string.IsNullOrWhiteSpace(existingUser.PasswordHash) || existingUser.PasswordHash == "hashed_pw"))
                {
                    existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, password);
                }
            }

            if (existingUsers.Count > 0)
                await context.SaveChangesAsync();

            //No duplicar el catálogo ni los datos de demostración.
            if (await context.Instruments.AnyAsync())
                return;

            // ================================================
            // CREACIÓN DE USUARIOS (Admin, Asesores, Clientes)
            // ================================================
            var adminId = Guid.NewGuid();
            var advisor1Id = Guid.NewGuid();
            var advisor2Id = Guid.NewGuid();
            var client1Id = Guid.NewGuid();
            var client2Id = Guid.NewGuid();
            var client3Id = Guid.NewGuid();
            var client4Id = Guid.NewGuid();
            var client5Id = Guid.NewGuid();

            var users = new List<User>
            {
                new() { Id = adminId, FullName = "Carlos Administrador", Email = "admin@inverflex.com", Role = UserRole.Admin },
                new() { Id = advisor1Id, FullName = "Sofía Asesor", Email = "asesor1@inverflex.com", Role = UserRole.Asesor },
                new() { Id = advisor2Id, FullName = "Marcos Asesor", Email = "asesor2@inverflex.com", Role = UserRole.Asesor },
                new() { Id = client1Id, FullName = "Juan Mora", Email = "cliente1@inverflex.com", Role = UserRole.Cliente },
                new() { Id = client2Id, FullName = "Ana Gómez", Email = "cliente2@inverflex.com", Role = UserRole.Cliente },
                new() { Id = client3Id, FullName = "Luis Pérez", Email = "cliente3@inverflex.com", Role = UserRole.Cliente },
                new() { Id = client4Id, FullName = "Jose Pereira", Email = "cliente4@inverflex.com", Role = UserRole.Cliente },
                new() { Id = client5Id, FullName = "Mará Mena", Email = "cliente5@inverflex.com", Role = UserRole.Cliente }
            };

            foreach (var demoUser in users)
                demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, demoPasswords[demoUser.Email]);

            await context.Users.AddRangeAsync(users);

            // ===========================
            // RELACIONES ASESOR - CLIENTE
            // ===========================
            var advisorClients = new List<AdvisorClient>
            {
                new() { AdvisorId = advisor1Id, ClientId = client1Id }, //Sofía atiende a Juan
                new() { AdvisorId = advisor1Id, ClientId = client2Id }, //Sofía atiende a Ana
                new() { AdvisorId = advisor2Id, ClientId = client3Id }  //Marcos atiende a Luis
            };

            await context.AdvisorClients.AddRangeAsync(advisorClients);

            // ================================================
            // CATÁLOGO DE INSTRUMENTOS (Acciones, ETFs, Bonos)
            // ================================================
            var instruments = new List<Instrument>
            {
                //Acciones
                new() { Id = Guid.NewGuid(), Ticker = "AAPL", Name = "Apple Inc.", Category = InstrumentCategory.Accion, Sector = "Tecnología", BasePrice = 185.50m },
                new() { Id = Guid.NewGuid(), Ticker = "MSFT", Name = "Microsoft Corp.", Category = InstrumentCategory.Accion, Sector = "Tecnología", BasePrice = 415.20m },
                new() { Id = Guid.NewGuid(), Ticker = "GOOGL", Name = "Alphabet Inc.", Category = InstrumentCategory.Accion, Sector = "Tecnología", BasePrice = 142.80m },
                new() { Id = Guid.NewGuid(), Ticker = "TSLA", Name = "Tesla Inc.", Category = InstrumentCategory.Accion, Sector = "Automotriz", BasePrice = 175.30m },
                new() { Id = Guid.NewGuid(), Ticker = "JPM", Name = "JPMorgan Chase & Co.", Category = InstrumentCategory.Accion, Sector = "Finanzas", BasePrice = 198.40m },
                new() { Id = Guid.NewGuid(), Ticker = "V", Name = "Visa Inc.", Category = InstrumentCategory.Accion, Sector = "Finanzas", BasePrice = 279.10m },
                new() { Id = Guid.NewGuid(), Ticker = "JNJ", Name = "Johnson & Johnson", Category = InstrumentCategory.Accion, Sector = "Salud", BasePrice = 156.70m },
                new() { Id = Guid.NewGuid(), Ticker = "PG", Name = "Procter & Gamble Co.", Category = InstrumentCategory.Accion, Sector = "Consumo", BasePrice = 161.20m },
                new() { Id = Guid.NewGuid(), Ticker = "NVDA", Name = "NVIDIA Corporation", Category = InstrumentCategory.Accion, Sector = "Tecnología", BasePrice = 880.50m },
                new() { Id = Guid.NewGuid(), Ticker = "AMZN", Name = "Amazon.com Inc.", Category = InstrumentCategory.Accion, Sector = "Consumo", BasePrice = 178.90m },

                //ETFs
                new() { Id = Guid.NewGuid(), Ticker = "SPY", Name = "SPDR S&P 500 ETF Trust", Category = InstrumentCategory.ETF, Sector = "Índice Global", BasePrice = 512.30m },
                new() { Id = Guid.NewGuid(), Ticker = "QQQ", Name = "Invesco QQQ Trust", Category = InstrumentCategory.ETF, Sector = "Tecnología ETF", BasePrice = 438.10m },
                new() { Id = Guid.NewGuid(), Ticker = "VTI", Name = "Vanguard Total Stock Market ETF", Category = InstrumentCategory.ETF, Sector = "Mercado Total", BasePrice = 254.60m },
                new() { Id = Guid.NewGuid(), Ticker = "DIA", Name = "SPDR Dow Jones Industrial Average ETF", Category = InstrumentCategory.ETF, Sector = "Industrial", BasePrice = 390.20m },
                new() { Id = Guid.NewGuid(), Ticker = "IWM", Name = "iShares Russell 2000 ETF", Category = InstrumentCategory.ETF, Sector = "Baja Capitalización", BasePrice = 202.40m },

                //Bonos
                new() { Id = Guid.NewGuid(), Ticker = "US10Y", Name = "Bono del Tesoro EEUU 10 Años", Category = InstrumentCategory.Bono, Sector = "Gobierno", BasePrice = 98.50m },
                new() { Id = Guid.NewGuid(), Ticker = "US30Y", Name = "Bono del Tesoro EEUU 30 Años", Category = InstrumentCategory.Bono, Sector = "Gobierno", BasePrice = 95.20m },
                new() { Id = Guid.NewGuid(), Ticker = "CORP-A", Name = "Bono Corporativo Grado de Inversión A", Category = InstrumentCategory.Bono, Sector = "Corporativo", BasePrice = 102.10m },
                new() { Id = Guid.NewGuid(), Ticker = "CORP-B", Name = "Bono Corporativo Alto Rendimiento B", Category = InstrumentCategory.Bono, Sector = "Corporativo", BasePrice = 89.75m },
                new() { Id = Guid.NewGuid(), Ticker = "MUNI-1", Name = "Bono Municipal Estata", Category = InstrumentCategory.Bono, Sector = "Municipal", BasePrice = 100.05m }
            };

            await context.Instruments.AddRangeAsync(instruments);

            // ==========================================
            // PORTAFOLIOS Y POSICIONES INICIALES
            // ==========================================
            var portfolioClient1 = new Portfolio
            {
                Id = Guid.NewGuid(),
                ClientId = client1Id,
                AvailableCash = 25000.00m,
                Positions =
                [
                    new PortfolioPosition { Id = Guid.NewGuid(), InstrumentId = instruments[0].Id, Quantity = 50m, AverageBuyPrice = 180.00m }, //AAPL
                    new PortfolioPosition { Id = Guid.NewGuid(), InstrumentId = instruments[10].Id, Quantity = 20m, AverageBuyPrice = 500.00m }  //SPY
                ]
            };

            var portfolioClient2 = new Portfolio
            {
                Id = Guid.NewGuid(),
                ClientId = client2Id,
                AvailableCash = 60000.00m, //Supera los 50k, ideal para probar órdenes de alto valor si compran más
                Positions =
                [
                    new PortfolioPosition { Id = Guid.NewGuid(), InstrumentId = instruments[1].Id, Quantity = 30m, AverageBuyPrice = 400.00m }  //MSFT
                ]
            };

            await context.Portfolios.AddRangeAsync(portfolioClient1, portfolioClient2);

            //Guardar todos los cambios en la base de datos SQLite
            await context.SaveChangesAsync();
        }
    }
}
