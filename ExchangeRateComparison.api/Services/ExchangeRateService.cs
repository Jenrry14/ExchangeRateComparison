// Services/ExchangeRateService.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Models;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparison.api.Services;

/// <summary>
/// Servicio principal que orquesta las consultas a múltiples APIs de tasas de cambio
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly IEnumerable<IExchangeRateClient> _clients;
    private readonly ILogger<ExchangeRateService> _logger;
    private static readonly object _statsLock = new();
    private static ServiceStatistics _statistics = new() { LastReset = DateTime.UtcNow };
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public ExchangeRateService(IEnumerable<IExchangeRateClient> clients, ILogger<ExchangeRateService> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la mejor tasa de cambio consultando todas las APIs disponibles en paralelo
    /// </summary>
    public async Task<BestRateResult> GetBestExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting exchange rate comparison for {SourceCurrency} to {TargetCurrency}, amount: {Amount}",
            request.SourceCurrency, request.TargetCurrency, request.Amount);

        // Filtrar solo clientes habilitados
        var enabledClients = _clients.Where(c => c.IsEnabled).ToList();
        
        if (!enabledClients.Any())
        {
            var error = "No exchange rate APIs are enabled";
            _logger.LogError(error);
            throw new InvalidOperationException(error);
        }

        _logger.LogDebug("Querying {ClientCount} enabled APIs: {ClientNames}", 
            enabledClients.Count, string.Join(", ", enabledClients.Select(c => c.ApiName)));

        // Ejecutar todas las consultas en paralelo
        var tasks = enabledClients.Select(client => 
            ExecuteClientRequestSafely(client, request, cancellationToken));
        
        var results = await Task.WhenAll(tasks);
        totalStopwatch.Stop();

        _logger.LogInformation("All API calls completed in {TotalTime}ms. Successful: {Successful}/{Total}",
            totalStopwatch.ElapsedMilliseconds, results.Count(r => r.Success), results.Length);

        // Actualizar estadísticas
        UpdateStatistics(results, totalStopwatch.Elapsed);

        var successfulResults = results.Where(r => r.Success).ToList();

        if (!successfulResults.Any())
        {
            var error = "All APIs failed to provide exchange rates";
            var errorDetails = string.Join("; ", results.Select(r => $"{r.ApiName}: {r.Error}"));
            _logger.LogError("{Error}. Details: {ErrorDetails}", error, errorDetails);
            throw new InvalidOperationException($"{error}. Details: {errorDetails}");
        }

        // Seleccionar la mejor oferta (mayor cantidad convertida = mejor para el cliente)
        var bestResult = successfulResults.OrderByDescending(r => r.ConvertedAmount).First();

        _logger.LogInformation("Best exchange rate found: {ApiName} - Rate: {Rate}, Amount: {ConvertedAmount} {TargetCurrency}, Response time: {ResponseTime}ms",
            bestResult.ApiName, bestResult.Rate, bestResult.ConvertedAmount, request.TargetCurrency, bestResult.ResponseTime.TotalMilliseconds);

        // Registrar estadística de mejor oferta
        lock (_statsLock)
        {
            if (_statistics.ApiStats.ContainsKey(bestResult.ApiName))
            {
                _statistics.ApiStats[bestResult.ApiName].BestOfferCount++;
            }
        }

        return new BestRateResult(bestResult, results.ToList(), totalStopwatch.Elapsed);
    }

    /// <summary>
    /// Ejecuta una consulta a un cliente de forma segura, capturando excepciones
    /// </summary>
    private async Task<ExchangeResponse> ExecuteClientRequestSafely(
        IExchangeRateClient client, 
        ExchangeRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetExchangeRateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{ApiName} request was cancelled", client.ApiName);
            return ExchangeResponse.CreateError(client.ApiName, "Request was cancelled", TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ApiName} threw unexpected exception", client.ApiName);
            return ExchangeResponse.CreateError(client.ApiName, $"Unexpected error: {ex.Message}", TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Verifica el estado de salud de todas las APIs
    /// </summary>
    public async Task<Dictionary<string, bool>> CheckApiHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking health of all APIs");
        
        var healthTasks = _clients.Select(async client =>
        {
            try
            {
                var isHealthy = await client.IsHealthyAsync(cancellationToken);
                return new { client.ApiName, IsHealthy = isHealthy };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for {ApiName}", client.ApiName);
                return new { client.ApiName, IsHealthy = false };
            }
        });

        var healthResults = await Task.WhenAll(healthTasks);
        return healthResults.ToDictionary(r => r.ApiName, r => r.IsHealthy);
    }

    /// <summary>
    /// Obtiene estadísticas del servicio
    /// </summary>
    public async Task<ServiceStatistics> GetStatisticsAsync()
    {
        await Task.CompletedTask; // Para mantener la interfaz async
        
        lock (_statsLock)
        {
            var stats = new ServiceStatistics
            {
                TotalRequests = _statistics.TotalRequests,
                SuccessfulRequests = _statistics.SuccessfulRequests,
                FailedRequests = _statistics.FailedRequests,
                AverageResponseTimeMs = _statistics.AverageResponseTimeMs,
                ApiStats = new Dictionary<string, ApiStatistics>(_statistics.ApiStats),
                Uptime = DateTime.UtcNow - _startTime,
                LastReset = _statistics.LastReset
            };
            
            return stats;
        }
    }

    /// <summary>
    /// Actualiza las estadísticas del servicio
    /// </summary>
    private void UpdateStatistics(ExchangeResponse[] results, TimeSpan totalTime)
    {
        lock (_statsLock)
        {
            _statistics.TotalRequests++;
            
            var hasSuccessful = results.Any(r => r.Success);
            if (hasSuccessful)
            {
                _statistics.SuccessfulRequests++;
            }
            else
            {
                _statistics.FailedRequests++;
            }

            // Actualizar tiempo promedio de respuesta
            var currentAvg = _statistics.AverageResponseTimeMs;
            var newTime = totalTime.TotalMilliseconds;
            _statistics.AverageResponseTimeMs = (currentAvg * (_statistics.TotalRequests - 1) + newTime) / _statistics.TotalRequests;

            // Actualizar estadísticas por API
            foreach (var result in results)
            {
                if (!_statistics.ApiStats.ContainsKey(result.ApiName))
                {
                    _statistics.ApiStats[result.ApiName] = new ApiStatistics { ApiName = result.ApiName };
                }

                var apiStats = _statistics.ApiStats[result.ApiName];
                apiStats.TotalRequests++;

                if (result.Success)
                {
                    apiStats.SuccessfulRequests++;
                    apiStats.LastSuccessfulResponse = DateTime.UtcNow;
                    
                    // Actualizar tiempo promedio de respuesta de la API
                    var apiCurrentAvg = apiStats.AverageResponseTimeMs;
                    var apiNewTime = result.ResponseTime.TotalMilliseconds;
                    apiStats.AverageResponseTimeMs = (apiCurrentAvg * (apiStats.TotalRequests - 1) + apiNewTime) / apiStats.TotalRequests;
                }
                else
                {
                    apiStats.LastError = result.Error;
                }
            }
        }
    }

    /// <summary>
    /// Reinicia las estadísticas del servicio
    /// </summary>
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _statistics = new ServiceStatistics { LastReset = DateTime.UtcNow };
        }
        
        _logger.LogInformation("Service statistics have been reset");
    }
}