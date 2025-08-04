// DTOs/ExchangeRequestDto.cs

using System.ComponentModel.DataAnnotations;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para solicitud de tasa de cambio
/// </summary>
public class ExchangeRequestDto
{
    /// <summary>
    /// Moneda origen (ejemplo: USD, EUR, GBP)
    /// </summary>
    [Required(ErrorMessage = "Source currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be exactly 3 characters")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be 3 uppercase letters")]
    public string SourceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Moneda destino (ejemplo: USD, EUR, GBP)
    /// </summary>
    [Required(ErrorMessage = "Target currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be exactly 3 characters")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be 3 uppercase letters")]
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad a convertir
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}