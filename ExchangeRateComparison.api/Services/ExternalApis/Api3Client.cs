// Services/ExternalApis/Api3Client.cs

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparison.api.Services.ExternalApis;

/// <summary>
/// Cliente para API3 - Formato JSON anidado
/// Input: {exchange: {sourceCurrency, targetCurrency, quantity}}
/// Output: {statusCode, message, data: {total}}
/// </summary>
public class Api3Client : IExchangeRateClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api3Client> _logger;
    private readonly ApiEndpoint _config;

    public string ApiName => "API3";
    public bool IsEnabled => _config.IsEnabled;

    public Api3Client(HttpClient httpClient, ILogger<Api3Client> logger, IOptions<ApiConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value.Api3;
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        
        // Configurar Basic Authentication
        if (_config.AuthType.ToLower() == "basic" && 
            !string.IsNullOrEmpty(_config.ApiKey) && 
            !string.IsNullOrEmpty(_config.ApiSecret))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_config.ApiKey}:{_config.ApiSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task<ExchangeResponse> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Making nested JSON request to {ApiName} for {From}-{To}", 
                ApiName, request.SourceCurrency, request.TargetCurrency);

            var payload = new
            {
                exchange = new
                {
                    sourceCurrency = request.SourceCurrency,
                    targetCurrency = request.TargetCurrency,
                    quantity = request.Amount
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_config.Url}/exchange", payload, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("{ApiName} returned Unauthorized", ApiName);
                return ExchangeResponse.CreateError(ApiName, "Authentication failed", stopwatch.Elapsed);
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Api3Response>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result == null)
            {
                throw new InvalidOperationException("Null response received from API");
            }

            if (result.StatusCode != 200)
            {
                throw new InvalidOperationException($"API returned error status {result.StatusCode}: {result.Message}");
            }

            if (result.Data?.Total == null || result.Data.Total <= 0)
            {
                throw new InvalidOperationException("Invalid total amount in API response");
            }

            var convertedAmount = result.Data.Total;
            var rate = convertedAmount / request.Amount;

            _logger.LogDebug("{ApiName} responded successfully with total {Total}", 
                ApiName, convertedAmount);

            return new ExchangeResponse(ApiName, rate, convertedAmount, stopwatch.Elapsed);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("{ApiName} request timed out", ApiName);
            return ExchangeResponse.CreateTimeout(ApiName, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ApiName} request failed", ApiName);
            return ExchangeResponse.CreateError(ApiName, ex.Message, stopwatch.Elapsed);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private record Api3Response(int StatusCode, string Message, Api3Data? Data);
    private record Api3Data(decimal Total);
}