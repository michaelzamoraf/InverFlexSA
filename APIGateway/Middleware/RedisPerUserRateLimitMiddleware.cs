using System.Globalization;
using StackExchange.Redis;

namespace APIGateway.Middleware;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Aplica un límite distribuido de solicitudes por usuario utilizando Redis como contador compartido.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class RedisPerUserRateLimitMiddleware : IMiddleware
{
    private const int RequestsPerMinute = 200;
    private static readonly LuaScript SlidingWindow = LuaScript.Prepare(
        "local now = tonumber(@now); " +
        "local window = tonumber(@window); " +
        "local limit = tonumber(@limit); " +
        "redis.call('ZREMRANGEBYSCORE', @key, '-inf', now - window); " +
        "local count = redis.call('ZCARD', @key); " +
        "if count >= limit then " +
        "  local oldest = redis.call('ZRANGE', @key, 0, 0, 'WITHSCORES'); " +
        "  return {0, count, tonumber(oldest[2])}; " +
        "end; " +
        "redis.call('ZADD', @key, now, @requestId); " +
        "redis.call('PEXPIRE', @key, window); " +
        "return {1, count + 1, 0};");

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPerUserRateLimitMiddleware> _logger;

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Inicializa el middleware con la conexión Redis y el logger.
    /// </summary>
    /// <param name="redis">Multiplexor Redis compartido por las instancias del Gateway.</param>
    /// <param name="logger">Logger de eventos operativos.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public RedisPerUserRateLimitMiddleware(
        IConnectionMultiplexer redis,
        ILogger<RedisPerUserRateLimitMiddleware> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Cuenta las solicitudes autenticadas dentro de una ventana de un minuto por usuario.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud.</param>
    /// <param name="next">Siguiente middleware.</param>
    /// -----------------------------------------------------------------------------------------------------------
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        //El endpoint de login debe poder emitir el token sin tener uno previamente.
        var requestPath = context.Request.Path.Value ?? string.Empty;
        if (requestPath.EndsWith("/Auth/token", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        //Ocelot realiza el challenge de rutas protegidas; no se cuenta tráfico anónimo.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirst("userId")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "El JWT no contiene el claim userId requerido." });
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var nowMilliseconds = now.ToUnixTimeMilliseconds();
        var windowMilliseconds = (long)TimeSpan.FromMinutes(1).TotalMilliseconds;
        var key = (RedisKey)$"inverflex:gateway:ratelimit:{userId}";

        try
        {
            var database = _redis.GetDatabase();

            RedisResult? scriptResult = await database.ScriptEvaluateAsync(
                SlidingWindow,
                new
                {
                    key,
                    now = nowMilliseconds,
                    window = windowMilliseconds,
                    limit = RequestsPerMinute,
                    requestId = Guid.NewGuid().ToString("N")
                });

            //Validar que Redis haya devuelto un resultado.
            if (scriptResult is null || scriptResult.IsNull)
            {
                throw new RedisException(
                    "Redis no devolvió un resultado válido al evaluar el límite de solicitudes.");
            }

            //La respuesta esperada del script Lua contiene tres elementos:
            //[0] = indicador de solicitud permitida (1 o 0).
            //[1] = cantidad de solicitudes registradas.
            //[2] = timestamp de la solicitud más antigua.
            RedisResult[]? result = (RedisResult[]?)scriptResult;

            //Validar la estructura de la respuesta antes de acceder a sus posiciones.
            if (result is null ||
                result.Length != 3 ||
                result[0].IsNull ||
                result[1].IsNull ||
                result[2].IsNull)
            {
                throw new RedisException(
                    "Redis devolvió una estructura de respuesta inválida para el rate limit.");
            }

            var allowed = (long)result[0] == 1;
            var count = (long)result[1];
            var oldestTimestamp = (long)result[2];

            context.Response.Headers["X-RateLimit-Limit"] = RequestsPerMinute.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, RequestsPerMinute - count).ToString(CultureInfo.InvariantCulture);

            if (!allowed)
            {
                var retryMilliseconds = Math.Max(1000, oldestTimestamp + windowMilliseconds - nowMilliseconds);
                var retrySeconds = (long)Math.Ceiling(retryMilliseconds / 1000d);
                context.Response.Headers["Retry-After"] = retrySeconds.ToString(CultureInfo.InvariantCulture);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Se excedió el límite de 200 solicitudes dentro de una ventana móvil de 60 segundos para este usuario."
                });
                return;
            }
        }
        catch (RedisException exception)
        {
            //Fail-closed: no se omite el límite cuando Redis no está disponible.
            _logger.LogError(exception, "No fue posible validar el rate limit distribuido para userId {UserId}.", userId);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "El servicio de control de tráfico no está disponible temporalmente."
            });
            return;
        }

        await next(context);
    }
}
