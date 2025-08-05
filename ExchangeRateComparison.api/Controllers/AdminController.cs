// Controllers/AdminController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using ExchangeRateComparison.api.Services;

namespace ExchangeRateComparison.api.Controllers;

/// <summary>
/// Controlador para administración y configuración de APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Administration")]
public class AdminController : ControllerBase
{
    private readonly IOptionsMonitor<ApiConfiguration> _config;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IApiConfigurationService _apiConfigService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IOptionsMonitor<ApiConfiguration> config,
        IExchangeRateService exchangeRateService,
        IApiConfigurationService apiConfigService,
        ILogger<AdminController> logger)
    {
        _config = config;
        _exchangeRateService = exchangeRateService;
        _apiConfigService = apiConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado actual de todas las APIs
    /// </summary>
    [HttpGet("apis/status")]
    public async Task<ActionResult<ApiStatusResponse>> GetApisStatus()
    {
        var config = _config.CurrentValue;
        var healthCheck = await _exchangeRateService.CheckApiHealthAsync();

        var apis = new List<ApiStatus>
        {
            new("API1", config.Api1.IsEnabled, config.Api1.Url, healthCheck.GetValueOrDefault("API1", false)),
            new("API2", config.Api2.IsEnabled, config.Api2.Url, healthCheck.GetValueOrDefault("API2", false)),
            new("API3", config.Api3.IsEnabled, config.Api3.Url, healthCheck.GetValueOrDefault("API3", false))
        };

        return Ok(new ApiStatusResponse(apis, DateTime.UtcNow));
    }

    /// <summary>
    /// Habilita o deshabilita una API específica
    /// </summary>
    [HttpPut("apis/{apiName}/toggle")]
    public async Task<ActionResult<ApiToggleResponse>> ToggleApi(
        [FromRoute] string apiName,
        [FromBody] ApiToggleRequest request)
    {
        try
        {
            _logger.LogInformation("Toggling {ApiName} to {Enabled}", apiName, request.Enabled);

            var result = await _apiConfigService.ToggleApiAsync(apiName, request.Enabled);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var response = new ApiToggleResponse(
                apiName,
                request.Enabled,
                $"API {apiName} has been {(request.Enabled ? "enabled" : "disabled")}",
                DateTime.UtcNow
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling API {ApiName}", apiName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Habilita o deshabilita múltiples APIs de una vez
    /// </summary>
    [HttpPut("apis/bulk-toggle")]
    public async Task<ActionResult<BulkToggleResponse>> BulkToggleApis([FromBody] BulkToggleRequest request)
    {
        var results = new List<ApiToggleResult>();

        foreach (var apiConfig in request.Apis)
        {
            try
            {
                var result = await _apiConfigService.ToggleApiAsync(apiConfig.ApiName, apiConfig.Enabled);
                results.Add(new ApiToggleResult(apiConfig.ApiName, apiConfig.Enabled, result.Success, result.ErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling API {ApiName}", apiConfig.ApiName);
                results.Add(new ApiToggleResult(apiConfig.ApiName, apiConfig.Enabled, false, ex.Message));
            }
        }

        var successCount = results.Count(r => r.Success);
        var response = new BulkToggleResponse(
            results,
            successCount,
            results.Count - successCount,
            DateTime.UtcNow
        );

        return Ok(response);
    }

    /// <summary>
    /// Resetea las estadísticas del servicio
    /// </summary>
    [HttpPost("statistics/reset")]
    public ActionResult<ResetStatisticsResponse> ResetStatistics()
    {
        try
        {
            _exchangeRateService.ResetStatistics();
            _logger.LogInformation("Statistics reset by admin request");

            return Ok(new ResetStatisticsResponse(
                "Statistics have been successfully reset",
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting statistics");
            return StatusCode(500, new { error = "Failed to reset statistics" });
        }
    }
}

// ===== DTOs para el AdminController =====

public record ApiToggleRequest([Required] bool Enabled);

public record ApiToggleResponse(
    string ApiName,
    bool Enabled,
    string Message,
    DateTime Timestamp
);

public record ApiStatus(
    string Name,
    bool IsEnabled,
    string Url,
    bool IsHealthy
);

public record ApiStatusResponse(
    List<ApiStatus> Apis,
    DateTime Timestamp
);

public record BulkToggleRequest(List<BulkApiConfig> Apis);

public record BulkApiConfig(
    [Required] string ApiName,
    [Required] bool Enabled
);

public record ApiToggleResult(
    string ApiName,
    bool Enabled,
    bool Success,
    string? ErrorMessage
);

public record BulkToggleResponse(
    List<ApiToggleResult> Results,
    int SuccessCount,
    int FailureCount,
    DateTime Timestamp
);

public record ResetStatisticsResponse(
    string Message,
    DateTime Timestamp
);