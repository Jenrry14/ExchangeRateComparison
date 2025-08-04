// DTOs/ErrorResponseDto.cs

using System;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para respuesta de error
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Mensaje de error
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// Detalles adicionales del error
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Timestamp del error
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Código de trazabilidad
    /// </summary>
    public string? TraceId { get; set; }
}