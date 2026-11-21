using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using APIInverFlexSA.Controllers;

namespace APIInverFlexSATest;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Pruebas de integración del flujo JWT y de autorización de endpoints administrativos.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class AuthenticationAuthorizationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inicializa las pruebas con el host HTTP de pruebas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public AuthenticationAuthorizationIntegrationTests(ApiWebApplicationFactory factory) => _factory = factory;

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que credenciales válidas emiten un JWT.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Token_ValidCredentials_ReturnsJwt()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/Auth/token", new LoginRequest("admin@test.local", "Password1!"));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(token?.Token));
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba el rechazo de credenciales inválidas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Token_InvalidPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/Auth/token", new LoginRequest("admin@test.local", "incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un endpoint protegido rechaza solicitudes anónimas.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AdminEndpoint_Anonymous_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/Admin/assign-client", new { AdvisorId = Guid.NewGuid(), ClientId = _factory.ClientId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un token de cliente no satisface la política admin.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AdminEndpoint_ClientRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "client@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/Admin/assign-client", new { AdvisorId = Guid.NewGuid(), ClientId = _factory.ClientId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un token admin alcanza la autorización por rol.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AdminEndpoint_AdminRole_ReachesEndpoint()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "admin@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/Admin/assign-client", new { AdvisorId = Guid.NewGuid(), ClientId = _factory.ClientId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un cliente no puede consultar la cartera de otro cliente.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Portfolio_OtherClient_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "client@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/v1/Portfolios/{_factory.OtherClientId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un asesor sin asignación persistida no puede consultar una cartera.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Portfolio_UnassignedAdvisor_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "advisor@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/v1/Portfolios/{_factory.ClientId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que el rol asesor puede acceder al grupo de endpoints de reportes.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AdvisorReport_AdvisorRole_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "advisor@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/Advisors/reports/performance?from=2026-01-01&to=2026-12-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Comprueba que un cliente no puede invocar el grupo de reportes de asesor.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AdvisorReport_ClientRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await GetToken(client, "client@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/Advisors/reports/performance?from=2026-01-01&to=2026-12-31");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Solicita un token JWT para el usuario indicado.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    private static async Task<string> GetToken(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/Auth/token", new LoginRequest(email, "Password1!"));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return token!.Token;
    }
}
