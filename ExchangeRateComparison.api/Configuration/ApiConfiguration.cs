// Configuration/ApiConfiguration.cs

using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateComparison.api.Configuration;
[ExcludeFromCodeCoverage]


/// <summary>
/// Configuración para las APIs externas de tasas de cambio
/// </summary>
public class ApiConfiguration
{
    public const string SectionName = "ExchangeRateApis";
    
    public ApiEndpoint Api1 { get; set; } = new();
    public ApiEndpoint Api2 { get; set; } = new();
    public ApiEndpoint Api3 { get; set; } = new();
}

/// <summary>
/// Configuración individual para cada API externa
/// </summary>

[ExcludeFromCodeCoverage]

public class ApiEndpoint
{
    /// <summary>
    /// URL base de la API
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Clave de API (usado para ApiKey y como username en Basic Auth)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Secreto de API (usado como password en Basic Auth)
    /// </summary>
    public string? ApiSecret { get; set; }
    
    /// <summary>
    /// Token Bearer para autenticación
    /// </summary>
    public string? BearerToken { get; set; }
    
    /// <summary>
    /// Tipo de autenticación: ApiKey, Bearer, Basic
    /// </summary>
    public string AuthType { get; set; } = "ApiKey";
    
    /// <summary>
    /// Timeout en segundos para las requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
    
    /// <summary>
    /// Indica si la API está habilitada
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}