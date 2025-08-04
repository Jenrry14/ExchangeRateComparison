// Services/ExternalApis/Api3Client.cs

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ExchangeRateComparison.api.Services.ExternalApis;
[ExcludeFromCodeCoverage]


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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string ApiName => "API3";
    public bool IsEnabled => _config.IsEnabled;

    public Api3Client(HttpClient httpClient, ILogger<Api3Client> logger, IOptions<ApiConfiguration> config, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value.Api3;
        _httpContextAccessor = httpContextAccessor;
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // Configuración base se hace en Program.cs
        // Solo configurar headers que no cambien dinámicamente
    }

    public async Task<ExchangeResponse> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Making nested JSON request to {ApiName} for {From}-{To}", 
                ApiName, request.SourceCurrency, request.TargetCurrency);

            // Obtener API key del HttpContext dinámicamente
            var httpContext = _httpContextAccessor?.HttpContext;
            var dynamicApiKey = httpContext?.Items["API3_KEY"]?.ToString();
            
            if (string.IsNullOrEmpty(dynamicApiKey))
            {
                _logger.LogWarning("{ApiName} - No API key provided in request", ApiName);
                return ExchangeResponse.CreateError(ApiName, "No API key provided", stopwatch.Elapsed);
            }

            var payload = new
            {
                exchange = new
                {
                    sourceCurrency = request.SourceCurrency,
                    targetCurrency = request.TargetCurrency,
                    quantity = request.Amount
                }
            };

            // Crear request con header dinámico
            var requestUri = $"{_config.Url}/exchange";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            httpRequestMessage.Headers.Add("X-API-Key", dynamicApiKey);
            httpRequestMessage.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
            
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
            var healthUri = $"{_config.Url}/health";
            var response = await _httpClient.GetAsync(healthUri, cancellationToken);
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