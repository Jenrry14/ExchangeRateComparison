// Models/ExchangeRate/ExchangeRequest.cs

using System;

namespace ExchangeRateComparison.api.Models.ExchangeRate;

/// <summary>
/// Solicitud de tasa de cambio de divisa
/// </summary>
public class ExchangeRequest
{
    public string SourceCurrency { get; private set; }
    public string TargetCurrency { get; private set; }
    public decimal Amount { get; private set; }

    public ExchangeRequest(string sourceCurrency, string targetCurrency, decimal amount)
    {
        ValidateInput(sourceCurrency, targetCurrency, amount);
        
        SourceCurrency = sourceCurrency.ToUpper().Trim();
        TargetCurrency = targetCurrency.ToUpper().Trim();
        Amount = amount;
    }

    private static void ValidateInput(string sourceCurrency, string targetCurrency, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(sourceCurrency))
            throw new ArgumentException("Source currency is required", nameof(sourceCurrency));
            
        if (string.IsNullOrWhiteSpace(targetCurrency))
            throw new ArgumentException("Target currency is required", nameof(targetCurrency));
            
        if (sourceCurrency.Trim().Length != 3)
            throw new ArgumentException("Source currency must be 3 characters", nameof(sourceCurrency));
            
        if (targetCurrency.Trim().Length != 3)
            throw new ArgumentException("Target currency must be 3 characters", nameof(targetCurrency));
            
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
            
        if (sourceCurrency.Trim().Equals(targetCurrency.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and target currencies must be different");
    }
}