// Services/ApiConfigurationService.cs

using Microsoft.Extensions.Options;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Services.Interfaces;
using System.Collections.Concurrent;

namespace ExchangeRateComparison.api.Services;

public interface IApiConfigurationService
{
    Task<ApiToggleResult> ToggleApiAsync(string apiName, bool enabled);
    bool IsApiEnabled(string apiName);
    ApiEndpoint? GetApiConfig(string apiName);
}

/// <summary>
/// Servicio para gestionar configuración dinámica de APIs
/// </summary>
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IOptionsMonitor<ApiConfiguration> _config;
    private readonly ILogger<ApiConfigurationService> _logger;
    
    // Cache en memoria para overrides dinámicos
    private static readonly ConcurrentDictionary<string, bool> _apiOverrides = new();

    public ApiConfigurationService(
        IOptionsMonitor<ApiConfiguration> config,
        ILogger<ApiConfigurationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Habilita o deshabilita una API específica en memoria
    /// </summary>
    public async Task<ApiToggleResult> ToggleApiAsync(string apiName, bool enabled)
    {
        await Task.CompletedTask; // Para mantener async

        var normalizedApiName = apiName.ToUpperInvariant();
        
        if (!IsValidApiName(normalizedApiName))
        {
            return new ApiToggleResult(apiName, enabled, false, $"API '{apiName}' no es válida. APIs válidas: API1, API2, API3");
        }

        try
        {
            // Guardar override en memoria
            _apiOverrides.AddOrUpdate(normalizedApiName, enabled, (key, oldValue) => enabled);
            
            _logger.LogInformation("API {ApiName} has been {Status} via dynamic configuration",
                apiName, enabled ? "enabled" : "disabled");

            return new ApiToggleResult(apiName, enabled, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling API {ApiName}", apiName);
            return new ApiToggleResult(apiName, enabled, false, ex.Message);
        }
    }

    /// <summary>
    /// Verifica si una API está habilitada (considerando overrides dinámicos)
    /// </summary>
    public bool IsApiEnabled(string apiName)
    {
        var normalizedApiName = apiName.ToUpperInvariant();
        
        // Primero verificar si hay override dinámico
        if (_apiOverrides.TryGetValue(normalizedApiName, out var overrideValue))
        {
            _logger.LogDebug("Using dynamic override for {ApiName}: {Enabled}", apiName, overrideValue);
            return overrideValue;
        }

        // Si no hay override, usar configuración del appsettings.json
        var config = _config.CurrentValue;
        return normalizedApiName switch
        {
            "API1" => config.Api1.IsEnabled,
            "API2" => config.Api2.IsEnabled,
            "API3" => config.Api3.IsEnabled,
            _ => false
        };
    }

    /// <summary>
    /// Obtiene la configuración de una API específica
    /// </summary>
    public ApiEndpoint? GetApiConfig(string apiName)
    {
        var config = _config.CurrentValue;
        return apiName.ToUpperInvariant() switch
        {
            "API1" => config.Api1,
            "API2" => config.Api2,
            "API3" => config.Api3,
            _ => null
        };
    }

    private static bool IsValidApiName(string apiName)
    {
        return apiName.ToUpperInvariant() is "API1" or "API2" or "API3";
    }
}

public record ApiToggleResult(
    string ApiName,
    bool Enabled,
    bool Success,
    string? ErrorMessage
);