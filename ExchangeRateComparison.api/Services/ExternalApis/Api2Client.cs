// Services/ExternalApis/Api2Client.cs

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ExchangeRateComparison.api.Configuration;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparison.api.Services.ExternalApis;

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

    public string ApiName => "API2";
    public bool IsEnabled => _config.IsEnabled;

    public Api2Client(HttpClient httpClient, ILogger<Api2Client> logger, IOptions<ApiConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value.Api2;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        
        // Configurar autenticación Bearer Token
        if (_config.AuthType.ToLower() == "bearer" && !string.IsNullOrEmpty(_config.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.BearerToken);
        }
    }

    public async Task<ExchangeResponse> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Making XML request to {ApiName} for {From}-{To}", 
                ApiName, request.SourceCurrency, request.TargetCurrency);

            var xmlPayload = BuildXmlPayload(request);
            var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

            var response = await _httpClient.PostAsync($"{_config.Url}/exchange", content, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("{ApiName} returned Unauthorized", ApiName);
                return ExchangeResponse.CreateError(ApiName, "Authentication failed", stopwatch.Elapsed);
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
            _logger.LogWarning("{ApiName} request timed out", ApiName);
            return ExchangeResponse.CreateTimeout(ApiName, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ApiName} request failed", ApiName);
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
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
