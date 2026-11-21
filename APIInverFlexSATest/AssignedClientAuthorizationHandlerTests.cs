using APIInverFlexSA.Domain.Entities;
using APIInverFlexSA.Infrastructure.Persistence;
using APIInverFlexSA.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas unitarias de autorización por rol, propietario y asignación asesor-cliente.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AssignedClientAuthorizationHandlerTests
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el administrador tiene acceso global.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Admin_CanAccessAnyClient()
    {
        var target = Guid.NewGuid();
        var result = await Authorize("admin", Guid.NewGuid(), target);

        Assert.True(result.HasSucceeded);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el cliente puede acceder a su propia cartera.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Client_CanAccessOwnPortfolio()
    {
        var client = Guid.NewGuid();
        var result = await Authorize("cliente", client, client);

        Assert.True(result.HasSucceeded);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un cliente no puede acceder a otra cartera.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Client_CannotAccessOtherPortfolio()
    {
        var result = await Authorize("cliente", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.HasSucceeded);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba el acceso de un asesor a un cliente asignado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Advisor_CanAccessAssignedClient()
    {
        var advisor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var result = await Authorize("asesor", advisor, client, new AdvisorClient { AdvisorId = advisor, ClientId = client });

        Assert.True(result.HasSucceeded);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba el rechazo de un asesor sin asignación.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Advisor_CannotAccessUnassignedClient()
    {
        var result = await Authorize("asesor", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.HasSucceeded);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba el rechazo cuando faltan claims necesarios.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task MissingClaims_FailsAuthorization()
    {
        await using var db = CreateDb(out var connection);
        var requirement = new AssignedClientRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(new ClaimsIdentity()), Guid.NewGuid());
        await new AssignedClientAuthorizationHandler(db).HandleAsync(context);

        Assert.True(context.HasFailed);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Evalúa la autorización de un usuario frente a un cliente objetivo.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static async Task<AuthorizationHandlerContext> Authorize(string role, Guid userId, Guid target, AdvisorClient? assignment = null)
    {
        await using var db = CreateDb(out var connection);
        if (assignment is not null)
        {
            db.Users.AddRange(
                new User { Id = assignment.AdvisorId, FullName = "Advisor", Email = "advisor@test.local" },
                new User { Id = assignment.ClientId, FullName = "Client", Email = "client@test.local" });
            await db.SaveChangesAsync();
            db.AdvisorClients.Add(assignment);
            await db.SaveChangesAsync();
        }

        var requirement = new AssignedClientRequirement();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role),
            new Claim("userId", userId.ToString())
        }, "test");
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(identity), target);
        await new AssignedClientAuthorizationHandler(db).HandleAsync(context);
        connection.Dispose();
        return context;
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Crea un contexto SQLite en memoria para una prueba aislada.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static AppDbContext CreateDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
