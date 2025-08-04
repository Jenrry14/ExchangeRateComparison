// DTOs/ExchangeOfferDto.cs

using System;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para una oferta individual de tasa de cambio
/// </summary>
public class ExchangeOfferDto
{
    /// <summary>
    /// Nombre de la API que proporcionó la oferta
    /// </summary>
    public string ApiName { get; set; } = string.Empty;
    
    /// <summary>
    /// Tasa de cambio (puede ser null si falló)
    /// </summary>
    public decimal? Rate { get; set; }
    
    /// <summary>
    /// Cantidad convertida (puede ser null si falló)
    /// </summary>
    public decimal? ConvertedAmount { get; set; }
    
    /// <summary>
    /// Tiempo de respuesta de la API en milisegundos
    /// </summary>
    public double ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Indica si la consulta fue exitosa
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Mensaje de error (solo si Success es false)
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Timestamp de la respuesta
    /// </summary>
    public DateTime Timestamp { get; set; }
}