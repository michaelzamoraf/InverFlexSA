using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Hospeda el API con SQLite en memoria y una configuración JWT exclusiva de pruebas.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Identificador del usuario administrador sembrado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public Guid AdminId { get; } = Guid.NewGuid();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Identificador del usuario cliente sembrado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public Guid ClientId { get; } = Guid.NewGuid();

    /// <summary>
    /// Identificador del segundo cliente usado para comprobar aislamiento entre carteras.
    /// </summary>
    public Guid OtherClientId { get; } = Guid.NewGuid();

    /// <summary>
    /// Identificador del asesor usado para comprobar restricciones por asignación.
    /// </summary>
    public Guid AdvisorId { get; } = Guid.NewGuid();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inicializa la fábrica de pruebas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public ApiWebApplicationFactory() => _connection.Open();

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Configura SQLite y los datos mínimos de autenticación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "test-key-that-is-at-least-32-characters-long");
        builder.UseSetting("Jwt:Issuer", "InverFlex.Tests");
        builder.UseSetting("Jwt:Audience", "InverFlex.Tests");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "InverFlex.Tests",
            ["Jwt:Audience"] = "InverFlex.Tests",
            ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            Seed(db);
        });
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Libera la conexión SQLite compartida.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inserta los usuarios mínimos utilizados por las pruebas de integración.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;
        var hasher = new PasswordHasher<User>();
        var admin = new User { Id = AdminId, Email = "admin@test.local", FullName = "Admin", Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, "Password1!");
        var client = new User { Id = ClientId, Email = "client@test.local", FullName = "Client", Role = UserRole.Cliente };
        client.PasswordHash = hasher.HashPassword(client, "Password1!");
        var otherClient = new User { Id = OtherClientId, Email = "other-client@test.local", FullName = "Other Client", Role = UserRole.Cliente };
        otherClient.PasswordHash = hasher.HashPassword(otherClient, "Password1!");
        var advisor = new User { Id = AdvisorId, Email = "advisor@test.local", FullName = "Advisor", Role = UserRole.Asesor };
        advisor.PasswordHash = hasher.HashPassword(advisor, "Password1!");
        db.Users.AddRange(admin, client, otherClient, advisor);
        db.SaveChanges();
    }
}
