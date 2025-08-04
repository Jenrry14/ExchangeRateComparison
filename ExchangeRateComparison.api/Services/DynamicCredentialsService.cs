using System.Diagnostics.CodeAnalysis;
using ExchangeRateComparison.api.Services.Interfaces;
using ExchangeRateComparison.api.Services.ExternalApis;

namespace ExchangeRateComparison.api.Services;
[ExcludeFromCodeCoverage]


public class DynamicCredentialsService : IDynamicCredentialsService
{
    private readonly IEnumerable<IExchangeRateClient> _clients;
    private readonly ILogger<DynamicCredentialsService> _logger;

    public DynamicCredentialsService(IEnumerable<IExchangeRateClient> clients, ILogger<DynamicCredentialsService> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task ConfigureApiCredentialsAsync(string? api1Key, string? api2Key, string? api3Key)
    {
        await Task.CompletedTask; 

        foreach (var client in _clients)
        {
            var key = client.ApiName switch
            {
                "API1" => api1Key,
                "API2" => api2Key, 
                "API3" => api3Key,
                _ => null
            };

            if (!string.IsNullOrEmpty(key))
            {
                _logger.LogDebug("Configuring dynamic credentials for {ApiName}", client.ApiName);
            }
        }
    }
}