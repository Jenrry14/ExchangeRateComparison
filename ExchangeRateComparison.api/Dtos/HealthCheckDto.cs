// DTOs/HealthCheckDto.cs


using System;
using System.Collections.Generic;
using ExchangeRateComparison.api.Dtos;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para health check
/// </summary>
public class HealthCheckDto
{
    /// <summary>
    /// Estado del servicio
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp del health check
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Nombre del servicio
    /// </summary>
    public string Service { get; set; } = "ExchangeRateService";
    
    /// <summary>
    /// Versión de la API
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Estado de las APIs externas
    /// </summary>
    public List<ApiHealthDto> ExternalApis { get; set; } = new();
}