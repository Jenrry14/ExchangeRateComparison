// DTOs/ApiEndpointsDto.cs
namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para endpoints disponibles de la API
/// </summary>
public class ApiEndpointsDto
{
    /// <summary>
    /// URL de documentación Swagger
    /// </summary>
    public string Documentation { get; set; } = "/swagger";
    
    /// <summary>
    /// Endpoint de health check
    /// </summary>
    public string Health { get; set; } = "/health";
    
    /// <summary>
    /// Endpoint principal para obtener mejor tasa
    /// </summary>
    public string BestRate { get; set; } = "POST /api/exchangerate/best-rate";
    
    /// <summary>
    /// Endpoint para estadísticas
    /// </summary>
    public string Statistics { get; set; } = "GET /api/exchangerate/statistics";
    
    /// <summary>
    /// Endpoint para divisas soportadas
    /// </summary>
    public string Currencies { get; set; } = "GET /api/exchangerate/currencies";
    
    /// <summary>
    /// Endpoint para health check detallado
    /// </summary>
    public string DetailedHealth { get; set; } = "GET /api/exchangerate/health";
}