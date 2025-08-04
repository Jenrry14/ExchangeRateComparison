// MockApis/Controllers/MockApi2Controller.cs

using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateComparison.api.MockApis.Controllers;

/// <summary>
/// Mock API2 - Simula API externa con formato XML
/// </summary>
[ApiController]
[Route("api2")]                     
[ApiExplorerSettings(IgnoreApi = true)]

public class MockApi2Controller : ControllerBase
{
    private readonly ILogger<MockApi2Controller> _logger;
    private static readonly Dictionary<string, decimal> MockRates = new()
    {
        ["USD-EUR"] = 0.87m, // Ligeramente diferentes para comparación
        ["USD-GBP"] = 0.76m,
        ["USD-JPY"] = 111.50m,
        ["EUR-USD"] = 1.15m,
        ["EUR-GBP"] = 0.89m,
        ["EUR-JPY"] = 128.25m,
        ["GBP-USD"] = 1.32m,
        ["GBP-EUR"] = 1.12m,
        ["GBP-JPY"] = 147.80m,
        ["JPY-USD"] = 0.0090m,
        ["JPY-EUR"] = 0.0078m,
        ["JPY-GBP"] = 0.0068m
    };

    public MockApi2Controller(ILogger<MockApi2Controller> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint principal para conversión de divisas (XML)
    /// </summary>
    [HttpPost("exchange")]
    [Consumes("application/xml")]
    [Produces("application/xml")]
    public async Task<ActionResult> Exchange()
    {
        // Validar autenticación Bearer
        if (!ValidateBearerToken())
        {
            _logger.LogWarning("API2 - Invalid or missing Bearer token");
            return Unauthorized("<?xml version=\"1.0\"?><XML><error>Invalid Bearer token</error></XML>");
        }

        // Simular tiempo de procesamiento
        await Task.Delay(Random.Shared.Next(100, 500));

        // Simular fallos ocasionales (7% de probabilidad)
        if (Random.Shared.NextDouble() < 0.07)
        {
            _logger.LogWarning("API2 simulating temporary failure");
            return StatusCode(500, "<?xml version=\"1.0\"?><XML><error>API2 temporarily unavailable</error></XML>");
        }

        try
        {
            using var reader = new StreamReader(Request.Body);
            var xmlContent = await reader.ReadToEndAsync();
            
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;
            
            var from = root?.Element("From")?.Value ?? "";
            var to = root?.Element("To")?.Value ?? "";
            var amountStr = root?.Element("Amount")?.Value ?? "0";

            if (!decimal.TryParse(amountStr, out var amount))
            {
                _logger.LogError("API2 - Invalid amount format: {Amount}", amountStr);
                return BadRequest("<?xml version=\"1.0\"?><XML><error>Invalid amount format</error></XML>");
            }

            var rate = GetRate(from, to);
            var result = rate * amount;

            _logger.LogInformation("API2 returning result {Result} for {From}-{To}, amount {Amount}", 
                result, from, to, amount);

            return Content($"<?xml version=\"1.0\"?><XML><Result>{result}</Result></XML>", "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API2 error processing XML request");
            return BadRequest("<?xml version=\"1.0\"?><XML><error>Invalid XML format</error></XML>");
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", api = "MockAPI2", timestamp = DateTime.UtcNow });
    }

    private bool ValidateBearerToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return false;
        }

        var token = authHeader.Substring(7);
        var validTokens = new[] { "demo-bearer-token", "test-bearer-token", "valid-token-2" };
        return validTokens.Contains(token);
    }

    private static decimal GetRate(string from, string to)
    {
        var key = $"{from.ToUpper()}-{to.ToUpper()}";
        
        if (MockRates.TryGetValue(key, out var rate))
        {
            var variation = (decimal)(Random.Shared.NextDouble() * 0.03 - 0.015); // ±1.5%
            return Math.Max(0.0001m, rate * (1 + variation));
        }

        var reverseKey = $"{to.ToUpper()}-{from.ToUpper()}";
        if (MockRates.TryGetValue(reverseKey, out var reverseRate))
        {
            var invertedRate = 1 / reverseRate;
            var variation = (decimal)(Random.Shared.NextDouble() * 0.03 - 0.015);
            return Math.Max(0.0001m, invertedRate * (1 + variation));
        }

        return 1m;
    }
}
