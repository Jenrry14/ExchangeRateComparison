// Models/ApiStatistics.cs

using System;

namespace ExchangeRateComparison.api.Models;

/// <summary>
/// Estadísticas específicas de una API (Modelo de Dominio)
/// </summary>
public class ApiStatistics
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
    /// Tasa de éxito (porcentaje)
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    
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
}