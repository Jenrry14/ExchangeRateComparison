// DTOs/ApiStatisticsDto.cs

using System;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para estadísticas específicas de una API
/// </summary>
public class ApiStatisticsDto
{
    /// <summary>
    /// Nombre de la API
    /// </summary>
    public string ApiName { get; set; } = string.Empty;
    
    /// <summary>
    /// Solicitudes totales a esta API
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
    /// Tasa de éxito (porcentaje)
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Tiempo promedio de respuesta
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Última respuesta exitosa
    /// </summary>
    public DateTime? LastSuccessfulResponse { get; set; }
    
    /// <summary>
    /// Último error registrado
    /// </summary>
    public string? LastError { get; set; }
    
    /// <summary>
    /// Cuántas veces esta API tuvo la mejor oferta
    /// </summary>
    public long BestOfferCount { get; set; }
    
    /// <summary>
    /// Porcentaje de veces que esta API tuvo la mejor oferta
    /// </summary>
    public double BestOfferPercentage { get; set; }
}