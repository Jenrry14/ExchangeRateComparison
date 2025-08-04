// DTOs/SupportedCurrenciesDto.cs

using System;
using System.Collections.Generic;

namespace ExchangeRateComparison.api.Dtos;

/// <summary>
/// DTO para respuesta de divisas soportadas
/// </summary>
public class SupportedCurrenciesDto
{
    /// <summary>
    /// Lista de códigos de divisas soportadas
    /// </summary>
    public List<CurrencyDto> Currencies { get; set; } = new();
    
    /// <summary>
    /// Total de divisas soportadas
    /// </summary>
    public int TotalCurrencies => Currencies.Count;
    
    /// <summary>
    /// Timestamp de cuando se generó la lista
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}