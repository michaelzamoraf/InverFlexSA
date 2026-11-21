using APIInverFlexSA.Infrastructure.Persistence;
using APIInverFlexSA.Security;
using APIInverFlexSA.Application.Features.Portfolios;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using APIInverFlexSA.Infrastructure.Repositories;
using APIInverFlexSA.Infrastructure.Services;
using APIInverFlexSA.Application.Services;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region DBContext

//Configuración de la cadena de conexión SQLite (creará un archivo local 'inverflex.db')
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=inverflex.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

//Registrar la interfaz para la inyección de dependencias
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Repositorios EF Core
builder.Services.AddScoped<IAdvisorClientRepository, AdvisorClientRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();

// Redis: caché distribuida compartida entre instancias del API.
var redisConfiguration = builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
    options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "InverFlex:";
});
builder.Services.AddScoped<IDistributedCacheService, RedisDistributedCacheService>();
builder.Services.AddScoped<IMarketPriceService, MockMarketPriceService>();
builder.Services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();

#endregion

//Controllers with Newtonsoft JSON (required for JSON Patch support)
builder.Services.AddControllers().AddNewtonsoftJson();

//Response Caching
builder.Services.AddResponseCaching();
//Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

#region API Versioning (URL segment: /api/v1/... y /api/v2/...)

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

#endregion

#region Autenticación: JWT Bearer (default) + API Key (esquema custom)

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                logger.LogError(context.Exception, "Error validando el JWT.");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                logger.LogWarning("JWT Challenge. Error: {Error}. Description: {Description}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.SchemeName, options =>
        {
            options.HeaderName = builder.Configuration["ApiKey:HeaderName"] ?? "X-API-Key";
            options.Value = builder.Configuration["ApiKey:Value"] ?? string.Empty;
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientePolicy", policy => policy.RequireRole("cliente"));
    options.AddPolicy("AsesorPolicy", policy => policy.RequireRole("asesor", "admin"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));
    options.AddPolicy("CanAccessClientPortfolio", policy =>
        policy.Requirements.Add(new AssignedClientRequirement()));
});

// Registrar el handler personalizado para validar propietario/asignación.
builder.Services.AddScoped<IAuthorizationHandler, AssignedClientAuthorizationHandler>();

// Registrar los handlers de MediatR contenidos en el ensamblado de Application.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetClientPortfolioQuery).Assembly));

#endregion

#region Swagger/OpenAPI

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("auth", new OpenApiInfo
    {
        Title = "InverFlex - Grupo Auth",
        Version = "v1",
        Description = "Endpoints para la autenticación en InverFlex."
    });

    c.SwaggerDoc("cliente", new OpenApiInfo
    {
        Title = "InverFlex - Grupo Cliente",
        Version = "v1",
        Description = "Endpoints orientados a clientes finales."
    });

    c.SwaggerDoc("asesor", new OpenApiInfo
    {
        Title = "InverFlex - Grupo Asesor",
        Version = "v1",
        Description = "Endpoints orientados a asesores financieros."
    });

    c.SwaggerDoc("admin", new OpenApiInfo
    {
        Title = "InverFlex - Grupo Admin",
        Version = "v1",
        Description = "Endpoints de administración."
    });

    //Cada documento contiene exclusivamente los endpoints asignados a su grupo.
    c.DocInclusionPredicate((documentName, apiDescription) =>
        string.Equals(apiDescription.GroupName, documentName, StringComparison.OrdinalIgnoreCase));

    //Configuración de seguridad JWT compartida por los documentos protegidos.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT generado en el endpoint de Auth."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });

    //Include XML comments for Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

#endregion

var app = builder.Build();

#region Carga de datos iniciales (Seeding) y creación de la base de datos

//Bloque de inicialización y Seeding de la base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        //Esto creará el archivo 'inverflex.db' y poblará los datos iniciales
        await DbInitializer.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar y poblar la base de datos.");
    }
}

#endregion

//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(c =>
    {
        //Registrar cada pestaña/documento en el menú desplegable de Swagger UI
        c.SwaggerEndpoint("/swagger/auth/swagger.json", "InverFlex - Auth");
        c.SwaggerEndpoint("/swagger/cliente/swagger.json", "InverFlex - Cliente");
        c.SwaggerEndpoint("/swagger/asesor/swagger.json", "InverFlex - Asesor");
        c.SwaggerEndpoint("/swagger/admin/swagger.json", "InverFlex - Admin");

        c.RoutePrefix = string.Empty; //Para que Swagger abra directo en la raíz (localhost:7219/)
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>
/// Expone el punto de entrada para hospedar la API en pruebas de integración.
/// </summary>
public partial class Program { }
