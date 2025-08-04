// MockApis/Controllers/MockApi3Controller.cs

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateComparison.api.MockApis.Controllers;

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
        // Validar autenticación Basic
        if (!ValidateBasicAuth())
        {
            _logger.LogWarning("API3 - Invalid or missing Basic authentication");
            return Unauthorized(new 
            { 
                statusCode = 401, 
                message = "Invalid Basic authentication", 
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

    private bool ValidateBasicAuth()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            return false;
        }

        try
        {
            var base64Credentials = authHeader.Substring(6);
            var credentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
            var parts = credentials.Split(':');
            
            if (parts.Length != 2)
                return false;

            var username = parts[0];
            var password = parts[1];

            // Simular validación de credenciales
            var validCredentials = new Dictionary<string, string>
            {
                ["demo-username"] = "demo-password",
                ["test-username"] = "test-password",
                ["user3"] = "pass3"
            };

            return validCredentials.ContainsKey(username) && validCredentials[username] == password;
        }
        catch
        {
            return false;
        }
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