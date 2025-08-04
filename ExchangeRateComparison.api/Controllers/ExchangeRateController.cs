// Controllers/ExchangeRateController.cs 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Dtos;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparison.api.Controllers;

/// <summary>
/// Controlador principal para operaciones de tasas de cambio
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateController> _logger;

    public ExchangeRateController(IExchangeRateService exchangeRateService, ILogger<ExchangeRateController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la mejor tasa de cambio comparando múltiples proveedores
    /// </summary>
    /// <param name="requestDto">Datos de la solicitud de cambio de divisa</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Mejor oferta encontrada con detalles de todas las APIs consultadas</returns>
    /// <response code="200">Mejor tasa de cambio encontrada</response>
    /// <response code="400">Datos de entrada inválidos</response>
    /// <response code="503">Todas las APIs externas fallaron</response>
    [HttpPost("best-rate")]
    [ProducesResponseType(typeof(BestRateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BestRateResponseDto>> GetBestRate(
        [FromBody] ExchangeRequestDto requestDto, 
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Processing exchange rate request: {SourceCurrency} to {TargetCurrency}, amount: {Amount} [TraceId: {TraceId}]",
                requestDto.SourceCurrency, requestDto.TargetCurrency, requestDto.Amount, traceId);

            // Validar entrada
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                _logger.LogWarning("Invalid request data: {Errors} [TraceId: {TraceId}]", 
                    string.Join(", ", errors), traceId);
                
                return BadRequest(new ErrorResponseDto
                {
                    Error = "Invalid request data",
                    Details = string.Join("; ", errors),
                    TraceId = traceId
                });
            }

            // Validación adicional de negocio
            if (requestDto.SourceCurrency.Equals(requestDto.TargetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ErrorResponseDto
                {
                    Error = "Source and target currencies must be different",
                    TraceId = traceId
                });
            }

            // Crear solicitud de dominio
            var request = new ExchangeRequest(requestDto.SourceCurrency, requestDto.TargetCurrency, requestDto.Amount);
            
            // Procesar solicitud
            var result = await _exchangeRateService.GetBestExchangeRateAsync(request, cancellationToken);

            // Mapear respuesta
            var response = MapToResponseDto(result);

            _logger.LogInformation("Exchange rate request completed successfully. Best offer: {ApiName} ({ConvertedAmount}) [TraceId: {TraceId}]",
                result.BestOffer.ApiName, result.BestOffer.ConvertedAmount, traceId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters [TraceId: {TraceId}]", traceId);
            return BadRequest(new ErrorResponseDto
            {
                Error = "Invalid request parameters",
                Details = ex.Message,
                TraceId = traceId
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "All APIs failed [TraceId: {TraceId}]", traceId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponseDto
            {
                Error = "Service temporarily unavailable",
                Details = ex.Message,
                TraceId = traceId
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was cancelled [TraceId: {TraceId}]", traceId);
            return StatusCode(StatusCodes.Status408RequestTimeout, new ErrorResponseDto
            {
                Error = "Request timeout",
                Details = "The request took too long to process",
                TraceId = traceId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing exchange rate request [TraceId: {TraceId}]", traceId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto
            {
                Error = "Internal server error",
                Details = "An unexpected error occurred while processing your request",
                TraceId = traceId
            });
        }
    }

    /// <summary>
    /// Verifica el estado de salud del servicio y APIs externas
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Estado de salud del servicio</returns>
    /// <response code="200">Servicio funcionando correctamente</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthCheckDto>> Health(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Health check requested");

            var apiHealthChecks = await _exchangeRateService.CheckApiHealthAsync(cancellationToken);
            var statistics = await _exchangeRateService.GetStatisticsAsync();

            var healthCheck = new HealthCheckDto
            {
                Status = "healthy",
                Service = "ExchangeRateService",
                Version = "1.0.0",
                ExternalApis = apiHealthChecks.Select(kvp => new ApiHealthDto
                {
                    Name = kvp.Key,
                    IsEnabled = true,
                    Status = kvp.Value ? "healthy" : "unhealthy",
                    LastResponseTimeMs = statistics.ApiStats.ContainsKey(kvp.Key) 
                        ? statistics.ApiStats[kvp.Key].AverageResponseTimeMs 
                        : null,
                    LastSuccessfulResponse = statistics.ApiStats.ContainsKey(kvp.Key)
                        ? statistics.ApiStats[kvp.Key].LastSuccessfulResponse
                        : null
                }).ToList()
            };

            return Ok(healthCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Ok(new HealthCheckDto
            {
                Status = "unhealthy",
                Service = "ExchangeRateService",
                Version = "1.0.0"
            });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de uso del servicio
    /// </summary>
    /// <returns>Estadísticas detalladas del servicio</returns>
    /// <response code="200">Estadísticas obtenidas correctamente</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ServiceStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceStatisticsDto>> GetStatistics()
    {
        try
        {
            var statistics = await _exchangeRateService.GetStatisticsAsync();
            
            var statisticsDto = new ServiceStatisticsDto
            {
                TotalRequests = statistics.TotalRequests,
                SuccessfulRequests = statistics.SuccessfulRequests,
                FailedRequests = statistics.FailedRequests,
                AverageResponseTimeMs = statistics.AverageResponseTimeMs,
                Uptime = statistics.Uptime.ToString(@"dd\.hh\:mm\:ss"),
                LastReset = statistics.LastReset,
                ApiStats = statistics.ApiStats.Select(kvp => new ApiStatisticsDto
                {
                    ApiName = kvp.Value.ApiName,
                    TotalRequests = kvp.Value.TotalRequests,
                    SuccessfulRequests = kvp.Value.SuccessfulRequests,
                    FailedRequests = kvp.Value.TotalRequests - kvp.Value.SuccessfulRequests,
                    SuccessRate = kvp.Value.SuccessRate,
                    AverageResponseTimeMs = kvp.Value.AverageResponseTimeMs,
                    LastSuccessfulResponse = kvp.Value.LastSuccessfulResponse,
                    LastError = kvp.Value.LastError,
                    BestOfferCount = kvp.Value.BestOfferCount,
                    BestOfferPercentage = statistics.TotalRequests > 0 
                        ? (double)kvp.Value.BestOfferCount / statistics.TotalRequests * 100 
                        : 0
                }).ToList()
            };
            
            return Ok(statisticsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto
            {
                Error = "Failed to retrieve statistics",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene las divisas soportadas
    /// </summary>
    /// <returns>Lista de divisas soportadas con información detallada</returns>
    [HttpGet("currencies")]
    [ProducesResponseType(typeof(SupportedCurrenciesDto), StatusCodes.Status200OK)]
    public ActionResult<SupportedCurrenciesDto> GetSupportedCurrencies()
    {
        var currencies = new SupportedCurrenciesDto
        {
            Currencies = new List<CurrencyDto>
            {
                new() { Code = "USD", Name = "US Dollar", Symbol = "$", Country = "United States" },
                new() { Code = "EUR", Name = "Euro", Symbol = "€", Country = "European Union" },
                new() { Code = "GBP", Name = "British Pound", Symbol = "£", Country = "United Kingdom" },
                new() { Code = "JPY", Name = "Japanese Yen", Symbol = "¥", Country = "Japan" },
                new() { Code = "CHF", Name = "Swiss Franc", Symbol = "CHF", Country = "Switzerland" },
                new() { Code = "CAD", Name = "Canadian Dollar", Symbol = "C$", Country = "Canada" },
                new() { Code = "AUD", Name = "Australian Dollar", Symbol = "A$", Country = "Australia" },
                new() { Code = "NZD", Name = "New Zealand Dollar", Symbol = "NZ$", Country = "New Zealand" },
                new() { Code = "SEK", Name = "Swedish Krona", Symbol = "kr", Country = "Sweden" },
                new() { Code = "NOK", Name = "Norwegian Krone", Symbol = "kr", Country = "Norway" },
                new() { Code = "DKK", Name = "Danish Krone", Symbol = "kr", Country = "Denmark" },
                new() { Code = "PLN", Name = "Polish Złoty", Symbol = "zł", Country = "Poland" },
                new() { Code = "CZK", Name = "Czech Koruna", Symbol = "Kč", Country = "Czech Republic" },
                new() { Code = "HUF", Name = "Hungarian Forint", Symbol = "Ft", Country = "Hungary" }
            }
        };

        return Ok(currencies);
    }

    /// <summary>
    /// Obtiene información general de la API
    /// </summary>
    /// <returns>Información sobre la API y endpoints disponibles</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiInfoDto), StatusCodes.Status200OK)]
    public ActionResult<ApiInfoDto> GetApiInfo()
    {
        var apiInfo = new ApiInfoDto
        {
            Service = "Exchange Rate Comparison API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Endpoints = new ApiEndpointsDto()
        };

        return Ok(apiInfo);
    }

    /// <summary>
    /// Mapea el resultado del servicio a DTO de respuesta
    /// </summary>
    private static BestRateResponseDto MapToResponseDto(BestRateResult result)
    {
        return new BestRateResponseDto
        {
            BestOffer = MapToOfferDto(result.BestOffer),
            AllResults = result.AllResults.Select(MapToOfferDto).ToList(),
            SuccessfulApis = result.SuccessfulApis,
            TotalApis = result.TotalApis,
            AverageRate = result.AverageRate,
            SuccessRate = result.SuccessRate,
            TotalProcessingTimeMs = result.TotalProcessingTime.TotalMilliseconds,
            ProcessedAt = result.ProcessedAt
        };
    }

    /// <summary>
    /// Mapea una respuesta de exchange a DTO de oferta
    /// </summary>
    private static ExchangeOfferDto MapToOfferDto(ExchangeResponse response)
    {
        return new ExchangeOfferDto
        {
            ApiName = response.ApiName,
            Rate = response.Rate,
            ConvertedAmount = response.ConvertedAmount,
            ResponseTimeMs = response.ResponseTime.TotalMilliseconds,
            Success = response.Success,
            Error = response.Error,
            Timestamp = response.Timestamp
        };
    }
}