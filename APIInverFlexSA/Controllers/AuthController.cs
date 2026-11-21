using APIInverFlexSA.Infrastructure.Persistence;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Controlador de autenticación para la generación o emisión del token
    /// </summary>
    /// <param name="configuration">Objeto de tipo IConfiguration que se recibe por inyección de dependencias</param>
    /// <param name="context">Objeto de tipo AppDbContext que se recibe por inyección de dependencias</param>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    public class AuthController(IConfiguration configuration, AppDbContext context) : ControllerBase
    {
        #region Atributos

        private readonly IConfiguration _configuration = configuration;
        private readonly AppDbContext _context = context;

        #endregion

        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Emite un JWT firmado para un usuario registrado.
        /// Ejemplo: POST /api/v1/Auth/token con Email y Password.
        /// </summary>
        /// <param name="request">Solicitud de Login para generar el Token</param>
        /// <returns>Token generado con las credenciales del usuario</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpPost("token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ApiExplorerSettings(GroupName = "auth")]
        public async Task<ActionResult<TokenResponse>> GetToken(LoginRequest request)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "JWT signing key is not configured." });

            //Validar si el usuario se encuentra registrado en el repositorio (Base de datos)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return Unauthorized(new { message = "Credenciales inválidas: usuario no encontrado." });

            //Verificar el hash persistido; nunca comparar la contraseña en texto plano.
            var passwordHasher = new PasswordHasher<Domain.Entities.User>();
            var passwordResult = passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Credenciales inválidas." });

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, request.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, request.Email!),
                new Claim(ClaimTypes.Role, RoleToClaim(user.Role)),
                new Claim("role", RoleToClaim(user.Role))
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new TokenResponse(tokenString, token.ValidTo));
        }

        private static string RoleToClaim(Domain.Entities.UserRole role) => role switch
        {
            Domain.Entities.UserRole.Cliente => "cliente",
            Domain.Entities.UserRole.Asesor => "asesor",
            Domain.Entities.UserRole.Admin => "admin",
            _ => throw new InvalidOperationException("Rol de usuario no soportado.")
        };

    #endregion
    }

    #region Records

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Solicitud de autenticación con correo y contraseña
    /// </summary>
    /// <param name="Email">Correo electrónico del usuario</param>
    /// <param name="Password">Contraseña del usuario</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record LoginRequest([Required] string Email, [Required] string Password);

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Respuesta con JWT emitido
    /// </summary>
    /// <param name="Token">Token generado o emitido</param>
    /// <param name="Expiration">Tiempo en que expira el token</param>
    /// -----------------------------------------------------------------------------------------------------------
    public record TokenResponse(string Token, DateTime Expiration);

    #endregion
}
