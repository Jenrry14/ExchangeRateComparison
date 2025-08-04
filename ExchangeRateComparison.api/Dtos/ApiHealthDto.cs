// DTOs/ApiHealthDto.cs

using System;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para estado de API externa
/// </summary>
public class ApiHealthDto
{
    /// <summary>
    /// Nombre de la API
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si la API está habilitada
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Estado actual de la API
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Último tiempo de respuesta en milisegundos
    /// </summary>
    public double? LastResponseTimeMs { get; set; }
    
    /// <summary>
    /// URL de la API
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// Última vez que respondió exitosamente
    /// </summary>
    public DateTime? LastSuccessfulResponse { get; set; }
}