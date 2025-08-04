// Services/ExternalApis/Api2Client.cs

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparison.api.Services.ExternalApis;
[ExcludeFromCodeCoverage]


/// <summary>
/// Cliente para API2 - Formato XML
/// Input: <XML><From>USD</From><To>EUR</To><Amount>100</Amount></XML>
/// Output: <XML><Result>85</Result></XML>
/// </summary>
public class Api2Client : IExchangeRateClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api2Client> _logger;
    private readonly ApiEndpoint _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string ApiName => "API2";
    public bool IsEnabled => _config.IsEnabled;

    public Api2Client(HttpClient httpClient, ILogger<Api2Client> logger, IOptions<ApiConfiguration> config, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value.Api2;
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
            _logger.LogDebug("Making XML request to {ApiName} for {From}-{To}", 
                ApiName, request.SourceCurrency, request.TargetCurrency);

            var httpContext = _httpContextAccessor?.HttpContext;
            var dynamicApiKey = httpContext?.Items["API2_KEY"]?.ToString();
            
            if (string.IsNullOrEmpty(dynamicApiKey))
            {
                _logger.LogWarning("{ApiName} - No API key provided in request", ApiName);
                return ExchangeResponse.CreateError(ApiName, "No API key provided", stopwatch.Elapsed);
            }

            var xmlPayload = BuildXmlPayload(request);

            //  Crear request con header dinámico
            var requestUri = $"{_config.Url}/exchange";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            httpRequestMessage.Headers.Add("X-API-Key", dynamicApiKey);
            httpRequestMessage.Content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

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

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var convertedAmount = ParseXmlResponse(responseContent);
            
            if (convertedAmount <= 0)
            {
                throw new InvalidOperationException("Invalid converted amount received");
            }
            
            var rate = convertedAmount / request.Amount;

            _logger.LogDebug("{ApiName} responded successfully with converted amount {Amount}", 
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

    private static string BuildXmlPayload(ExchangeRequest request)
    {
        return $"<XML><From>{request.SourceCurrency}</From><To>{request.TargetCurrency}</To><Amount>{request.Amount}</Amount></XML>";
    }

    private static decimal ParseXmlResponse(string xmlContent)
    {
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var resultElement = doc.Root?.Element("Result");
            
            if (resultElement == null)
            {
                throw new InvalidOperationException("Result element not found in XML response");
            }
            
            if (!decimal.TryParse(resultElement.Value, out var result))
            {
                throw new InvalidOperationException($"Invalid numeric value in XML response: {resultElement.Value}");
            }
                
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse XML response: {ex.Message}", ex);
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
}