// Services/Interfaces/IExchangeRateService.cs (CORREGIDO)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Models;
using ExchangeRateComparison.api.Models.ExchangeRate;

namespace ExchangeRateComparison.api.Services.Interfaces;

/// <summary>
/// Servicio principal para comparar tasas de cambio de múltiples APIs
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Obtiene la mejor tasa de cambio comparando múltiples APIs externas
    /// </summary>
    /// <param name="request">Solicitud con los datos de conversión</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado con la mejor oferta y detalles de todas las APIs</returns>
    Task<BestRateResult> GetBestExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica el estado de todas las APIs externas
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Estado de salud de cada API</returns>
    Task<Dictionary<string, bool>> CheckApiHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene estadísticas de uso del servicio
    /// </summary>
    /// <returns>Estadísticas de rendimiento y uso</returns>
    Task<ServiceStatistics> GetStatisticsAsync(); 
    
    /// <summary>
    /// Reinicia las estadísticas del servicio
    /// </summary>
    void ResetStatistics();
}