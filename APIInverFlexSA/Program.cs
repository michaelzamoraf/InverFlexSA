using APIInverFlexSA.Infrastructure.Persistence;
using APIInverFlexSA.Application.Features.Portfolios;
using APIInverFlexSA.Application.Interfaces;
using APIInverFlexSA.Domain.Interfaces;
using APIInverFlexSA.Infrastructure.Repositories;
using APIInverFlexSA.Infrastructure.Services;
using APIInverFlexSA.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

#region Contexto de base de datos y repositorios

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=inverflex.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IAdvisorClientRepository, AdvisorClientRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
builder.Services.AddScoped<IMarketPriceService, MockMarketPriceService>();
builder.Services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();
builder.Services.AddSingleton<IDistributedCacheService, NoOpDistributedCacheService>();

#endregion

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddResponseCaching();
builder.Services.AddOpenApi();

#region Versionamiento de API

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

#region MediatR

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetClientPortfolioQuery).Assembly));

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
    c.DocInclusionPredicate((documentName, apiDescription) =>
        string.Equals(apiDescription.GroupName, documentName, StringComparison.OrdinalIgnoreCase));
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar y poblar la base de datos.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/cliente/swagger.json", "InverFlex - Cliente");
        c.SwaggerEndpoint("/swagger/asesor/swagger.json", "InverFlex - Asesor");
        c.SwaggerEndpoint("/swagger/admin/swagger.json", "InverFlex - Admin");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.Run();

/// <summary>
/// Expone el punto de entrada para hospedar la API en pruebas de integración.
/// </summary>
public partial class Program { }
