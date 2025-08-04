// MockApis/Controllers/MockApi3Controller.cs

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateComparison.api.MockApis.Controllers;
[ExcludeFromCodeCoverage]


/// <summary>
/// Mock API3 - Simula API externa con formato JSON anidado
/// </summary>
[ApiController]
[Route("api3")]                             
[ApiExplorerSettings(IgnoreApi = true)] 

public class MockApi3Controller : ControllerBase
{
    private readonly ILogger<MockApi3Controller> _logger;
    private static readonly Dictionary<string, decimal> MockRates = new()
    {
        ["USD-EUR"] = 0.86m, 
        ["USD-GBP"] = 0.77m,
        ["USD-JPY"] = 109.75m,
        ["EUR-USD"] = 1.16m,
        ["EUR-GBP"] = 0.90m,
        ["EUR-JPY"] = 127.50m,
        ["GBP-USD"] = 1.30m,
        ["GBP-EUR"] = 1.11m,
        ["GBP-JPY"] = 142.50m,
        ["JPY-USD"] = 0.0091m,
        ["JPY-EUR"] = 0.0079m,
        ["JPY-GBP"] = 0.0070m
    };

    public MockApi3Controller(ILogger<MockApi3Controller> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint principal para conversión de divisas (JSON anidado)
    /// </summary>
    [HttpPost("exchange")]
    public async Task<ActionResult> Exchange([FromBody] Api3Request request)
    {
        // Validar autenticación API Key (igual que API1 y API2)
        if (!ValidateApiKey())
        {
            _logger.LogWarning("API3 - Invalid or missing API key");
            return Unauthorized(new 
            { 
                statusCode = 401, 
                message = "Invalid API key", 
                data = (object?)null 
            });
        }

        // Simular tiempo de procesamiento
        await Task.Delay(Random.Shared.Next(150, 600));

        // Simular fallos ocasionales (6% de probabilidad)
        if (Random.Shared.NextDouble() < 0.06)
        {
            _logger.LogWarning("API3 simulating temporary failure");
            return StatusCode(500, new 
            { 
                statusCode = 500, 
                message = "API3 temporarily unavailable", 
                data = (object?)null 
            });
        }

        // Simular rate limiting (2% de probabilidad)
        if (Random.Shared.NextDouble() < 0.02)
        {
            _logger.LogWarning("API3 simulating rate limit");
            return StatusCode(429, new 
            { 
                statusCode = 429, 
                message = "Rate limit exceeded", 
                data = (object?)null,
                retryAfter = 60
            });
        }

        // Simular respuesta de mantenimiento (1% de probabilidad)
        if (Random.Shared.NextDouble() < 0.01)
        {
            _logger.LogWarning("API3 simulating maintenance mode");
            return StatusCode(503, new 
            { 
                statusCode = 503, 
                message = "Service under maintenance, please try again later", 
                data = (object?)null 
            });
        }

        var exchange = request.Exchange;
        var rate = GetRate(exchange.SourceCurrency, exchange.TargetCurrency);
        var total = rate * exchange.Quantity;

        _logger.LogInformation("API3 returning total {Total} for {SourceCurrency}-{TargetCurrency}, quantity {Quantity}", 
            total, exchange.SourceCurrency, exchange.TargetCurrency, exchange.Quantity);

        return Ok(new
        {
            statusCode = 200,
            message = "Success",
            data = new { total },
            metadata = new
            {
                processingTime = Random.Shared.Next(50, 200),
                rateSource = "MockExchangeProvider",
                timestamp = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", api = "MockAPI3", timestamp = DateTime.UtcNow });
    }

    private bool ValidateApiKey()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return false;
        }

        // Simular validación de API keys válidas para API3
        var validKeys = new[] { "demo-api-key-3", "test-api-key-3", "valid-key-3" };
        return validKeys.Contains(apiKey.ToString());
    }

    private static decimal GetRate(string from, string to)
    {
        var key = $"{from.ToUpper()}-{to.ToUpper()}";
        
        if (MockRates.TryGetValue(key, out var rate))
        {
            var variation = (decimal)(Random.Shared.NextDouble() * 0.025 - 0.0125); // ±1.25%
            return Math.Max(0.0001m, rate * (1 + variation));
        }

        var reverseKey = $"{to.ToUpper()}-{from.ToUpper()}";
        if (MockRates.TryGetValue(reverseKey, out var reverseRate))
        {
            var invertedRate = 1 / reverseRate;
            var variation = (decimal)(Random.Shared.NextDouble() * 0.025 - 0.0125);
            return Math.Max(0.0001m, invertedRate * (1 + variation));
        }

        return 1m;
    }

    public record Api3Request(Api3Exchange Exchange);
    public record Api3Exchange(string SourceCurrency, string TargetCurrency, decimal Quantity);
}