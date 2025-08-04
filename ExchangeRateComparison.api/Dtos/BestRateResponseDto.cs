// DTOs/BestRateResponseDto.cs

using System;
using System.Collections.Generic;
using ExchangeRateComparison.api.Dtos;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para respuesta con la mejor tasa de cambio
/// </summary>
public class BestRateResponseDto
{
    /// <summary>
    /// Mejor oferta encontrada
    /// </summary>
    public ExchangeOfferDto BestOffer { get; set; } = null!;
    
    /// <summary>
    /// Resultados de todas las APIs consultadas
    /// </summary>
    public List<ExchangeOfferDto> AllResults { get; set; } = new();
    
    /// <summary>
    /// Número de APIs que respondieron exitosamente
    /// </summary>
    public int SuccessfulApis { get; set; }
    
    /// <summary>
    /// Total de APIs consultadas
    /// </summary>
    public int TotalApis { get; set; }
    
    /// <summary>
    /// Tasa promedio de todas las APIs exitosas
    /// </summary>
    public decimal? AverageRate { get; set; }
    
    /// <summary>
    /// Porcentaje de APIs exitosas
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Tiempo total de procesamiento en milisegundos
    /// </summary>
    public double TotalProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Timestamp de cuando se procesó la solicitud
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}