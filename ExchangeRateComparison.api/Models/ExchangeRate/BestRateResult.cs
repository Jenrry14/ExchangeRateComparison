// Models/ExchangeRate/BestRateResult.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace ExchangeRateComparison.api.Models.ExchangeRate;

/// <summary>
/// Resultado final con la mejor oferta de tasa de cambio
/// </summary>
public class BestRateResult
{
    public ExchangeResponse BestOffer { get; }
    public List<ExchangeResponse> AllResults { get; }
    public int SuccessfulApis => AllResults.Count(r => r.Success);
    public int TotalApis => AllResults.Count;
    public decimal? AverageRate => AllResults.Where(r => r.Success && r.Rate.HasValue).Select(r => r.Rate!.Value).DefaultIfEmpty(0).Average();
    public TimeSpan TotalProcessingTime { get; }
    public DateTime ProcessedAt { get; }

    public BestRateResult(ExchangeResponse bestOffer, List<ExchangeResponse> allResults, TimeSpan totalProcessingTime)
    {
        BestOffer = bestOffer;
        AllResults = allResults;
        TotalProcessingTime = totalProcessingTime;
        ProcessedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Indica si al menos una API respondió exitosamente
    /// </summary>
    public bool HasSuccessfulResponse => SuccessfulApis > 0;
    
    /// <summary>
    /// Porcentaje de APIs que respondieron exitosamente
    /// </summary>
    public double SuccessRate => TotalApis > 0 ? (double)SuccessfulApis / TotalApis * 100 : 0;
    
    /// <summary>
    /// Obtiene el resumen de resultados para logging
    /// </summary>
    public string GetSummary()
    {
        return $"Best offer: {BestOffer.ApiName} ({BestOffer.ConvertedAmount:F2}) | " +
               $"Success rate: {SuccessfulApis}/{TotalApis} ({SuccessRate:F1}%) | " +
               $"Processing time: {TotalProcessingTime.TotalMilliseconds:F0}ms";
    }
}