using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
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
/// Cliente para API1 - Formato JSON simple
/// Input: {from, to, value}
/// Output: {rate}
/// </summary>
public class Api1Client : IExchangeRateClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api1Client> _logger;
    private readonly ApiEndpoint _config;

    public string ApiName => "API1";
    public bool IsEnabled => _config.IsEnabled;

    public Api1Client(HttpClient httpClient, ILogger<Api1Client> logger, IOptions<ApiConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value.Api1;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        
        // Configurar autenticación según el tipo
        switch (_config.AuthType.ToLower())
        {
            case "apikey":
                if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
                    if (!string.IsNullOrEmpty(_config.ApiSecret))
                    {
                        _httpClient.DefaultRequestHeaders.Add("X-API-Secret", _config.ApiSecret);
                    }
                }
                break;
                
            case "bearer":
                if (!string.IsNullOrEmpty(_config.BearerToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.BearerToken);
                }
                break;
        }
    }

    public async Task<ExchangeResponse> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Making request to {ApiName} for {From}-{To}, amount: {Amount}", 
                ApiName, request.SourceCurrency, request.TargetCurrency, request.Amount);

            var payload = new
            {
                from = request.SourceCurrency,
                to = request.TargetCurrency,
                value = request.Amount
            };

            var response = await _httpClient.PostAsJsonAsync($"{_config.Url}/exchange", payload, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("{ApiName} returned Unauthorized - check API credentials", ApiName);
                return ExchangeResponse.CreateError(ApiName, "Authentication failed - invalid credentials", stopwatch.Elapsed);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("{ApiName} returned Too Many Requests - rate limit exceeded", ApiName);
                return ExchangeResponse.CreateError(ApiName, "Rate limit exceeded", stopwatch.Elapsed);
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Api1Response>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result?.Rate == null || result.Rate <= 0)
            {
                throw new InvalidOperationException("Invalid rate received from API");
            }

            var convertedAmount = result.Rate * request.Amount;
            
            _logger.LogDebug("{ApiName} responded successfully in {ElapsedMs}ms with rate {Rate}", 
                ApiName, stopwatch.ElapsedMilliseconds, result.Rate);

            return new ExchangeResponse(ApiName, result.Rate, convertedAmount, stopwatch.Elapsed);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("{ApiName} request timed out after {ElapsedMs}ms", 
                ApiName, stopwatch.ElapsedMilliseconds);
            return ExchangeResponse.CreateTimeout(ApiName, stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "{ApiName} HTTP request failed in {ElapsedMs}ms", 
                ApiName, stopwatch.ElapsedMilliseconds);
            return ExchangeResponse.CreateError(ApiName, $"HTTP error: {ex.Message}", stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "{ApiName} JSON parsing failed", ApiName);
            return ExchangeResponse.CreateError(ApiName, "Invalid JSON response", stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ApiName} request failed in {ElapsedMs}ms", 
                ApiName, stopwatch.ElapsedMilliseconds);
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

    private record Api1Response(decimal Rate);
}