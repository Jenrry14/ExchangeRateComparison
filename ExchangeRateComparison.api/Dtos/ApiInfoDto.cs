// DTOs/ApiInfoDto.cs

using System;
using ExchangeRateComparison.api.Dtos;


namespace ExchangeRateComparison.api.Dtos;


/// <summary>
/// DTO para información general de la API
/// </summary>
public class ApiInfoDto
{
    /// <summary>
    /// Nombre del servicio
    /// </summary>
    public string Service { get; set; } = "Exchange Rate Comparison API";
    
    /// <summary>
    /// Versión de la API
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Entorno de ejecución
    /// </summary>
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp actual del servidor
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// URLs de documentación y endpoints
    /// </summary>
    public ApiEndpointsDto Endpoints { get; set; } = new();
}