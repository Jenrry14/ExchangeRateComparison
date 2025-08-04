// DTOs/ServiceStatisticsDto.cs

using System;
using System.Collections.Generic;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para estadísticas del servicio
/// </summary>
public class ServiceStatisticsDto
{
    /// <summary>
    /// Total de solicitudes procesadas
    /// </summary>
    public long TotalRequests { get; set; }
    
    /// <summary>
    /// Solicitudes exitosas
    /// </summary>
    public long SuccessfulRequests { get; set; }
    
    /// <summary>
    /// Solicitudes fallidas
    /// </summary>
    public long FailedRequests { get; set; }
    
    /// <summary>
    /// Tasa de éxito general (porcentaje)
    /// </summary>
    public double OverallSuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    
    /// <summary>
    /// Tiempo promedio de respuesta en milisegundos
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Estadísticas por API
    /// </summary>
    public List<ApiStatisticsDto> ApiStats { get; set; } = new();
    
    /// <summary>
    /// Tiempo de actividad del servicio
    /// </summary>
    public string Uptime { get; set; } = string.Empty;
    
    /// <summary>
    /// Última vez que se reiniciaron las estadísticas
    /// </summary>
    public DateTime LastReset { get; set; }
    
    /// <summary>
    /// Timestamp de cuando se generaron estas estadísticas
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}