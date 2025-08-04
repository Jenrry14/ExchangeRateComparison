// Program.cs

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Services;
using ExchangeRateComparison.api.Services.ExternalApis;
using ExchangeRateComparison.api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE SERVICIOS =====

// Servicios básicos de Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ AGREGAR: HttpContextAccessor para acceder al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Configuración de Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Exchange Rate Comparison API", 
        Version = "v1",
        Description = "API para comparar tasas de cambio de múltiples proveedores y obtener la mejor oferta para clientes bancarios",
        Contact = new()
        {
            Name = "Exchange Rate Team",
            Email = "exchangerate@banreservas.com"
        }
    });

    // Excluir controladores Mock de la documentación
    c.DocInclusionPredicate((docName, description) =>
    {
        var controllerName = description.ActionDescriptor.RouteValues["controller"];
        return !controllerName?.Contains("MockApi") == true;
    });

    // Incluir comentarios XML en Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    //  Configurar headers de autenticación separados para cada API
    c.AddSecurityDefinition("Api1Key", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API1-Key",
        Description = "API Key para API1 (ej: demo-api-key-1)"
    });

    c.AddSecurityDefinition("Api2Key", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API2-Key",
        Description = "API Key para API2 (ej: demo-api-key-2)"
    });

    c.AddSecurityDefinition("Api3Key", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API3-Key",
        Description = "API Key para API3 (ej: demo-api-key-3)"
    });

    //  Requerir todas las keys en los endpoints
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference 
                { 
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                    Id = "Api1Key" 
                }
            },
            new string[] {}
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference 
                { 
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                    Id = "Api2Key" 
                }
            },
            new string[] {}
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference 
                { 
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                    Id = "Api3Key" 
                }
            },
            new string[] {}
        }
    });
});

// ===== CONFIGURACIÓN DE APIs EXTERNAS =====

// Registrar configuración de APIs externas
builder.Services.Configure<ApiConfiguration>(
    builder.Configuration.GetSection(ApiConfiguration.SectionName));

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurar niveles de log según el entorno
if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// ===== CONFIGURACIÓN DE HTTP CLIENTS CON POLÍTICAS DE RESILENCIA =====

// Políticas de Polly para resilencia
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            var logger = context.GetLogger();
            if (outcome.Exception != null)
            {
                logger?.LogWarning("HTTP retry {RetryCount} after {Delay}ms due to: {Exception}",
                    retryCount, timespan.TotalMilliseconds, outcome.Exception.Message);
            }
            else if (outcome.Result != null)
            {
                logger?.LogWarning("HTTP retry {RetryCount} after {Delay}ms due to status: {StatusCode}",
                    retryCount, timespan.TotalMilliseconds, outcome.Result.StatusCode);
            }
        });

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (exception, duration) =>
        {
            Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds} seconds");
        },
        onReset: () =>
        {
            Console.WriteLine("Circuit breaker reset");
        });

// Cliente para API1 (JSON)
builder.Services.AddHttpClient<Api1Client>("Api1Client", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<ApiConfiguration>>();
    var api1Config = config.Value.Api1;
    
    client.BaseAddress = new Uri(api1Config.Url);
    client.Timeout = TimeSpan.FromSeconds(api1Config.TimeoutSeconds);
    
    // Headers comunes
    client.DefaultRequestHeaders.Add("User-Agent", "ExchangeRateComparison/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false,
    MaxConnectionsPerServer = 10
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Cliente para API2 (XML)
builder.Services.AddHttpClient<Api2Client>("Api2Client", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<ApiConfiguration>>();
    var api2Config = config.Value.Api2;
    
    client.BaseAddress = new Uri(api2Config.Url);
    client.Timeout = TimeSpan.FromSeconds(api2Config.TimeoutSeconds);
    
    client.DefaultRequestHeaders.Add("User-Agent", "ExchangeRateComparison/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/xml");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false,
    MaxConnectionsPerServer = 10
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Cliente para API3 (JSON anidado)
builder.Services.AddHttpClient<Api3Client>("Api3Client", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<ApiConfiguration>>();
    var api3Config = config.Value.Api3;
    
    client.BaseAddress = new Uri(api3Config.Url);
    client.Timeout = TimeSpan.FromSeconds(api3Config.TimeoutSeconds);
    
    client.DefaultRequestHeaders.Add("User-Agent", "ExchangeRateComparison/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false,
    MaxConnectionsPerServer = 10
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// ===== REGISTRO DE SERVICIOS DE DOMINIO =====

// Registrar clientes de APIs externas
builder.Services.AddScoped<IExchangeRateClient, Api1Client>();
builder.Services.AddScoped<IExchangeRateClient, Api2Client>();
builder.Services.AddScoped<IExchangeRateClient, Api3Client>();

// Registrar servicio principal
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

//  Servicio para credenciales dinámicas
builder.Services.AddScoped<IDynamicCredentialsService, DynamicCredentialsService>();

// ===== CONFIGURACIÓN DE HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddCheck<ExchangeRateHealthCheck>("exchange-rate-apis");

// Registrar el health check personalizado
builder.Services.AddScoped<ExchangeRateHealthCheck>();

// ===== CONFIGURACIÓN DE CORS (para desarrollo) =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDevelopment", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== CONFIGURACIÓN DE VALIDACIÓN =====
builder.Services.AddOptions<ApiConfiguration>()
    .Bind(builder.Configuration.GetSection(ApiConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ===== CONSTRUCCIÓN DE LA APLICACIÓN =====
var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE =====

// Swagger en todos los entornos (para testing y producción)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Rate Comparison API V1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
    c.DocumentTitle = "Exchange Rate API - Banreservas";
    c.DefaultModelsExpandDepth(-1); // Colapsar modelos por defecto
    c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
});

//  Redirigir raíz a Swagger si no está funcionando
app.MapGet("/", () => Results.Redirect("/index.html")).ExcludeFromDescription();

// Middleware de manejo de errores
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Middleware de seguridad
app.UseHttpsRedirection();

// CORS solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowDevelopment");
}

// Middleware de autorización (preparado para futuras implementaciones)
app.UseAuthorization();

// Health checks
app.UseHealthChecks("/health");

// Mapear controladores
app.MapControllers();

// ===== ENDPOINTS ADICIONALES =====

// ✅ ACTUALIZADO: Endpoint de información de la API (cambiar ruta para no conflicto)
app.MapGet("/api-info", () => new
{
    Service = "Exchange Rate Comparison API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Documentation = "/index.html", // ✅ Actualizar a la URL que funciona
    Health = "/health",
    Endpoints = new
    {
        BestRate = "POST /api/exchangerate/best-rate",
        Health = "GET /api/exchangerate/health",
        Statistics = "GET /api/exchangerate/statistics",
        Currencies = "GET /api/exchangerate/currencies",
        Info = "GET /api/exchangerate/info"
    }
}).WithName("GetApiInfo").WithTags("Information");

// Endpoint de error para producción
app.MapGet("/error", () => Results.Problem("An error occurred while processing your request."))
   .WithName("Error").ExcludeFromDescription();

// ===== EVENTOS DE APLICACIÓN =====

// Log de inicio de la aplicación
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("=== Exchange Rate Comparison API Started ===");
    logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("API Documentation: {DocumentationUrl}", 
        app.Environment.IsDevelopment() ? "http://localhost:5000" : "/swagger");
    
    // Log de configuración de APIs
    using var scope = app.Services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiConfiguration>>();
    logger.LogInformation("APIs configured: API1={Api1Enabled}, API2={Api2Enabled}, API3={Api3Enabled}",
        config.Value.Api1.IsEnabled, config.Value.Api2.IsEnabled, config.Value.Api3.IsEnabled);
});

// Log de cierre de la aplicación
app.Lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Exchange Rate Comparison API is shutting down...");
});

// ===== EJECUTAR LA APLICACIÓN =====
app.Run();

// ===== EXTENSIONES Y CLASES AUXILIARES =====

/// <summary>
/// Health check personalizado para verificar APIs externas
/// </summary>
public class ExchangeRateHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateHealthCheck> _logger;

    public ExchangeRateHealthCheck(IServiceProvider serviceProvider, ILogger<ExchangeRateHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var exchangeService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            
            var apiHealth = await exchangeService.CheckApiHealthAsync(cancellationToken);
            var healthyApis = apiHealth.Count(kvp => kvp.Value);
            var totalApis = apiHealth.Count;

            var data = new Dictionary<string, object>
            {
                ["total_apis"] = totalApis,
                ["healthy_apis"] = healthyApis,
                ["health_percentage"] = totalApis > 0 ? (double)healthyApis / totalApis * 100 : 0,
                ["apis"] = apiHealth
            };

            if (healthyApis == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "All external APIs are unhealthy", data: data);
            }

            if (healthyApis < totalApis)
            {
                return HealthCheckResult.Degraded(
                    $"Only {healthyApis}/{totalApis} APIs are healthy", data: data);
            }

            return HealthCheckResult.Healthy(
                "All external APIs are healthy", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy(
                "Health check failed", ex);
        }
    }
}

/// <summary>
/// Extensiones para configuración de Polly
/// </summary>
public static class PollyExtensions
{
    public static ILogger? GetLogger(this Polly.Context context)
    {
        if (context.TryGetValue("logger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}

/// <summary>
/// Interfaz para servicio de credenciales dinámicas
/// </summary>
public interface IDynamicCredentialsService
{
    Task ConfigureApiCredentialsAsync(string? api1Key, string? api2Key, string? api3Key);
}

/// <summary>
/// Servicio para manejar credenciales dinámicas
/// </summary>
public class DynamicCredentialsService : IDynamicCredentialsService
{
    private readonly IEnumerable<IExchangeRateClient> _clients;
    private readonly ILogger<DynamicCredentialsService> _logger;

    public DynamicCredentialsService(IEnumerable<IExchangeRateClient> clients, ILogger<DynamicCredentialsService> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task ConfigureApiCredentialsAsync(string? api1Key, string? api2Key, string? api3Key)
    {
        await Task.CompletedTask; // Para mantener async

        foreach (var client in _clients)
        {
            var key = client.ApiName switch
            {
                "API1" => api1Key,
                "API2" => api2Key, 
                "API3" => api3Key,
                _ => null
            };

            if (!string.IsNullOrEmpty(key))
            {
                _logger.LogDebug("Dynamic credentials available for {ApiName}", client.ApiName);
            }
            else
            {
                _logger.LogWarning("No credentials provided for {ApiName}", client.ApiName);
            }
        }
    }
}

// Hacer la clase Program accesible para testing
[ExcludeFromCodeCoverage]
public partial class Program { }