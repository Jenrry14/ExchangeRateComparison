// Models/ExchangeRate/ExchangeResponse.cs

using System;

namespace ExchangeRateComparison.api.Models.ExchangeRate;

/// <summary>
/// Respuesta de una API externa con la tasa de cambio
/// </summary>
public class ExchangeResponse
{
    public string ApiName { get; }
    public decimal? Rate { get; }
    public decimal? ConvertedAmount { get; }
    public TimeSpan ResponseTime { get; }
    public bool Success { get; }
    public string? Error { get; }
    public DateTime Timestamp { get; }

    /// <summary>
    /// Constructor para respuesta exitosa
    /// </summary>
    public ExchangeResponse(string apiName, decimal rate, decimal convertedAmount, TimeSpan responseTime)
    {
        ApiName = apiName;
        Rate = rate;
        ConvertedAmount = convertedAmount;
        ResponseTime = responseTime;
        Success = true;
        Error = null;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor para respuesta con error
    /// </summary>
    private ExchangeResponse(string apiName, string error, TimeSpan responseTime)
    {
        ApiName = apiName;
        Rate = null;
        ConvertedAmount = null;
        ResponseTime = responseTime;
        Success = false;
        Error = error;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Crear una respuesta de error
    /// </summary>
    public static ExchangeResponse CreateError(string apiName, string error, TimeSpan responseTime)
        => new(apiName, error, responseTime);
        
    /// <summary>
    /// Crear una respuesta de timeout
    /// </summary>
    public static ExchangeResponse CreateTimeout(string apiName, TimeSpan responseTime)
        => new(apiName, "Request timeout", responseTime);
        
    /// <summary>
    /// Crear una respuesta de API no disponible
    /// </summary>
    public static ExchangeResponse CreateUnavailable(string apiName, TimeSpan responseTime)
        => new(apiName, "API temporarily unavailable", responseTime);
}