using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace APIInverFlexSA.Security
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Opciones de autenticación mediante una clave enviada en cabecera.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Nombre del esquema de autenticación.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public const string SchemeName = "ApiKey";

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Nombre de la cabecera que contiene la clave.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public string HeaderName { get; set; } = "X-API-Key";

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Valor esperado de la clave.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public string Value { get; set; } = string.Empty;
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Valida claves API usando comparación de tiempo constante.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inicializa el handler de autenticación.
        /// </summary>
        /// <param name="options">Opciones monitorizadas.</param>
        /// <param name="logger">Fábrica de logging.</param>
        /// <param name="encoder">Codificador de URL.</param>
        /// -----------------------------------------------------------------------------------------------------------
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Permite realizar el manejo de la autenticación de los usuarios.
        /// </summary>
        /// <returns></returns>
        /// -----------------------------------------------------------------------------------------------------------
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyValues))
                return Task.FromResult(AuthenticateResult.Fail("API Key header not found."));

            var providedKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(providedKey))
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

            // Constant-time comparison to prevent timing attacks.
            // FixedTimeEquals requires equal-length spans; compare lengths first (not in constant time,
            // but length leakage is acceptable compared to content leakage).
            var expectedKeyBytes = System.Text.Encoding.UTF8.GetBytes(Options.Value);
            var providedKeyBytes = System.Text.Encoding.UTF8.GetBytes(providedKey);
            if (expectedKeyBytes.Length != providedKeyBytes.Length ||
                !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedKeyBytes, providedKeyBytes))
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}