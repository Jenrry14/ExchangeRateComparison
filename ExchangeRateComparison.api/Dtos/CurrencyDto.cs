// DTOs/CurrencyDto.cs
namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para información de una divisa
/// </summary>
public class CurrencyDto
{
    /// <summary>
    /// Código de la divisa (ISO 4217)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre completo de la divisa
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Símbolo de la divisa
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// País o región asociada
    /// </summary>
    public string Country { get; set; } = string.Empty;
}