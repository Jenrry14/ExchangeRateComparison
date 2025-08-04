// MockApis/Controllers/MockApi1Controller.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparison.api.MockApis.Controllers;
[ExcludeFromCodeCoverage]


/// <summary>
/// Mock API1 - Simula API externa con formato JSON simple
/// </summary>
[ApiController]
[Route("api1")]                            
[ApiExplorerSettings(IgnoreApi = true)] 

public class MockApi1Controller : ControllerBase
{
    private readonly ILogger<MockApi1Controller> _logger;
    private static readonly Dictionary<string, decimal> MockRates = new()
    {
        ["USD-EUR"] = 0.85m,
        ["USD-GBP"] = 0.75m,
        ["USD-JPY"] = 110.25m,
        ["EUR-USD"] = 1.18m,
        ["EUR-GBP"] = 0.88m,
        ["EUR-JPY"] = 129.50m,
        ["GBP-USD"] = 1.33m,
        ["GBP-EUR"] = 1.14m,
        ["GBP-JPY"] = 146.75m,
        ["JPY-USD"] = 0.009m,
        ["JPY-EUR"] = 0.0077m,
        ["JPY-GBP"] = 0.0068m
    };

    public MockApi1Controller(ILogger<MockApi1Controller> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint principal para conversión de divisas (JSON simple)
    /// </summary>
    [HttpPost("exchange")]
    public async Task<ActionResult> Exchange([FromBody] Api1Request request)
    {
        // Validar autenticación
        if (!ValidateApiKey())
        {
            _logger.LogWarning("API1 - Invalid or missing API key");
            return Unauthorized(new { error = "Invalid API key" });
        }

        // Simular tiempo de procesamiento
        await Task.Delay(Random.Shared.Next(50, 300));

        // Simular fallos ocasionales (5% de probabilidad)
        if (Random.Shared.NextDouble() < 0.05)
        {
            _logger.LogWarning("API1 simulating temporary failure");
            return StatusCode(500, new { error = "API1 temporarily unavailable" });
        }

        // Simular rate limiting (2% de probabilidad)
        if (Random.Shared.NextDouble() < 0.02)
        {
            _logger.LogWarning("API1 simulating rate limit");
            return StatusCode(429, new { error = "Rate limit exceeded", retryAfter = 60 });
        }

        var rate = GetRate(request.From, request.To);
        
        _logger.LogInformation("API1 returning rate {Rate} for {From}-{To}, value: {Value}", 
            rate, request.From, request.To, request.Value);

        return Ok(new { rate });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", api = "MockAPI1", timestamp = DateTime.UtcNow });
    }

    private bool ValidateApiKey()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return false;
        }

        // Simular validación de API keys válidas
        var validKeys = new[] { "demo-api-key-1", "test-api-key-1", "valid-key-1" };
        return validKeys.Contains(apiKey.ToString());
    }

    private static decimal GetRate(string from, string to)
    {
        var key = $"{from.ToUpper()}-{to.ToUpper()}";
        
        if (MockRates.TryGetValue(key, out var rate))
        {
            // Agregar variación pequeña para simular mercado real
            var variation = (decimal)(Random.Shared.NextDouble() * 0.02 - 0.01); // ±1%
            return Math.Max(0.0001m, rate * (1 + variation));
        }

        var reverseKey = $"{to.ToUpper()}-{from.ToUpper()}";
        if (MockRates.TryGetValue(reverseKey, out var reverseRate))
        {
            var invertedRate = 1 / reverseRate;
            var variation = (decimal)(Random.Shared.NextDouble() * 0.02 - 0.01);
            return Math.Max(0.0001m, invertedRate * (1 + variation));
        }

        // Tasa por defecto si no se encuentra
        return 1m;
    }

    public record Api1Request(string From, string To, decimal Value);
}