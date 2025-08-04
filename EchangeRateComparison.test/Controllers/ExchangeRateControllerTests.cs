using ExchangeRateComparison.api.Controllers;
using ExchangeRateComparison.api.Dtos;
using ExchangeRateComparison.api.Models;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace ExchangeRateComparison.test.Controllers;

public class ExchangeRateControllerTests
{
    private readonly Mock<IExchangeRateService> _serviceMock;
    private readonly Mock<IDynamicCredentialsService> _credentialsMock;
    private readonly Mock<ILogger<ExchangeRateController>> _loggerMock;
    private readonly ExchangeRateController _controller;
    private readonly Mock<HttpContext> _httpContextMock;

    public ExchangeRateControllerTests()
    {
        _serviceMock = new Mock<IExchangeRateService>();
        _credentialsMock = new Mock<IDynamicCredentialsService>();
        _loggerMock = new Mock<ILogger<ExchangeRateController>>();
        _httpContextMock = new Mock<HttpContext>();
        
        // ✅ NUEVO: Constructor actualizado con IDynamicCredentialsService
        _controller = new ExchangeRateController(
            _serviceMock.Object, 
            _credentialsMock.Object, 
            _loggerMock.Object);

        // ✅ NUEVO: Configurar HttpContext mock para headers
        SetupHttpContext();
    }

    private void SetupHttpContext()
    {
        var headersMock = new Mock<IHeaderDictionary>();
        var requestMock = new Mock<HttpRequest>();
        var itemsMock = new Dictionary<object, object?>();

        // Simular headers de API keys
        headersMock.Setup(h => h["X-API1-Key"]).Returns(new StringValues("demo-api-key-1"));
        headersMock.Setup(h => h["X-API2-Key"]).Returns(new StringValues("demo-api-key-2"));
        headersMock.Setup(h => h["X-API3-Key"]).Returns(new StringValues("demo-api-key-3"));

        requestMock.Setup(r => r.Headers).Returns(headersMock.Object);
        _httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);
        _httpContextMock.Setup(c => c.Items).Returns(itemsMock);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContextMock.Object
        };
    }

    [Fact]
    public async Task GetBestRate_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        var bestResponse = new ExchangeResponse("API2", 0.87m, 87m, TimeSpan.FromMilliseconds(200));
        var allResponses = new List<ExchangeResponse>
        {
            new("API1", 0.85m, 85m, TimeSpan.FromMilliseconds(150)),
            bestResponse,
            new("API3", 0.86m, 86m, TimeSpan.FromMilliseconds(180))
        };

        var serviceResult = new BestRateResult(bestResponse, allResponses, TimeSpan.FromMilliseconds(250));

        _serviceMock.Setup(x => x.GetBestExchangeRateAsync(It.IsAny<ExchangeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        _credentialsMock.Setup(x => x.ConfigureApiCredentialsAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<BestRateResponseDto>();

        var responseDto = okResult.Value as BestRateResponseDto;
        responseDto!.BestOffer.ApiName.Should().Be("API2");
        responseDto.BestOffer.ConvertedAmount.Should().Be(87m);
        responseDto.SuccessfulApis.Should().Be(3);
        responseDto.TotalApis.Should().Be(3);

        _credentialsMock.Verify(x => x.ConfigureApiCredentialsAsync(
            "demo-api-key-1", "demo-api-key-2", "demo-api-key-3"), Times.Once);
    }

    [Fact]
    public async Task GetBestRate_WithNoApiKeys_ShouldReturnUnauthorized()
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        var headersMock = new Mock<IHeaderDictionary>();
        var requestMock = new Mock<HttpRequest>();
        
        headersMock.Setup(h => h["X-API1-Key"]).Returns(new StringValues());
        headersMock.Setup(h => h["X-API2-Key"]).Returns(new StringValues());
        headersMock.Setup(h => h["X-API3-Key"]).Returns(new StringValues());

        requestMock.Setup(r => r.Headers).Returns(headersMock.Object);
        _httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        var errorResponse = unauthorizedResult!.Value as ErrorResponseDto;
        errorResponse!.Error.Should().Be("Authentication required");
    }

    [Theory]
    [InlineData("", "EUR", 100)]
    [InlineData("USD", "", 100)]
    [InlineData("USD", "EUR", 0)]
    [InlineData("USD", "EUR", -100)]
    public async Task GetBestRate_WithInvalidRequest_ShouldReturnBadRequest(
        string sourceCurrency, string targetCurrency, decimal amount)
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = sourceCurrency,
            TargetCurrency = targetCurrency,
            Amount = amount
        };

        // Simular ModelState inválido
        if (string.IsNullOrEmpty(sourceCurrency))
            _controller.ModelState.AddModelError("SourceCurrency", "SourceCurrency is required");
        if (string.IsNullOrEmpty(targetCurrency))
            _controller.ModelState.AddModelError("TargetCurrency", "TargetCurrency is required");
        if (amount <= 0)
            _controller.ModelState.AddModelError("Amount", "Amount must be greater than 0");

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBestRate_WithSameCurrencies_ShouldReturnBadRequest()
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = "USD",
            TargetCurrency = "USD",
            Amount = 100
        };

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        var errorResponse = badRequestResult!.Value as ErrorResponseDto;
        errorResponse!.Error.Should().Be("Source and target currencies must be different");
    }

    [Fact]
    public async Task GetBestRate_WhenServiceThrowsInvalidOperationException_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        _serviceMock.Setup(x => x.GetBestExchangeRateAsync(It.IsAny<ExchangeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("All APIs failed"));

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
        
        var errorResponse = objectResult.Value as ErrorResponseDto;
        errorResponse!.Error.Should().Be("Service temporarily unavailable");
    }

    [Fact]
    public async Task GetBestRate_WhenServiceThrowsOperationCanceledException_ShouldReturnRequestTimeout()
    {
        // Arrange
        var requestDto = new ExchangeRequestDto
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        _serviceMock.Setup(x => x.GetBestExchangeRateAsync(It.IsAny<ExchangeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.GetBestRate(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(408);
    }

    [Fact]
    public async Task Health_ShouldReturnHealthCheckDto()
    {
        // Arrange
        var apiHealthChecks = new Dictionary<string, bool>
        {
            ["API1"] = true,
            ["API2"] = true,
            ["API3"] = false
        };

        var statistics = new ServiceStatistics
        {
            ApiStats = new Dictionary<string, ApiStatistics>
            {
                ["API1"] = new ApiStatistics { ApiName = "API1", AverageResponseTimeMs = 150 },
                ["API2"] = new ApiStatistics { ApiName = "API2", AverageResponseTimeMs = 200 },
                ["API3"] = new ApiStatistics { ApiName = "API3", AverageResponseTimeMs = 180 }
            }
        };

        _serviceMock.Setup(x => x.CheckApiHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiHealthChecks);
        _serviceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.Health(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<HealthCheckDto>();

        var healthDto = okResult.Value as HealthCheckDto;
        healthDto!.Status.Should().Be("healthy");
        healthDto.ExternalApis.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnServiceStatisticsDto()
    {
        // Arrange
        var statistics = new ServiceStatistics
        {
            TotalRequests = 100,
            SuccessfulRequests = 90,
            FailedRequests = 10,
            AverageResponseTimeMs = 250,
            Uptime = TimeSpan.FromHours(24),
            LastReset = DateTime.UtcNow.AddHours(-24),
            ApiStats = new Dictionary<string, ApiStatistics>
            {
                ["API1"] = new ApiStatistics 
                { 
                    ApiName = "API1", 
                    TotalRequests = 50,
                    SuccessfulRequests = 45,
                    AverageResponseTimeMs = 150,
                    BestOfferCount = 20
                }
            }
        };

        _serviceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ServiceStatisticsDto>();

        var statsDto = okResult.Value as ServiceStatisticsDto;
        statsDto!.TotalRequests.Should().Be(100);
        statsDto.SuccessfulRequests.Should().Be(90);
        statsDto.FailedRequests.Should().Be(10);
    }

    [Fact]
    public void GetSupportedCurrencies_ShouldReturnCurrencyList()
    {
        // Act
        var result = _controller.GetSupportedCurrencies();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<SupportedCurrenciesDto>();

        var currenciesDto = okResult.Value as SupportedCurrenciesDto;
        currenciesDto!.Currencies.Should().NotBeEmpty();
        currenciesDto.Currencies.Should().Contain(c => c.Code == "USD");
        currenciesDto.Currencies.Should().Contain(c => c.Code == "EUR");
    }

    [Fact]
    public void GetApiInfo_ShouldReturnApiInfo()
    {
        // Act
        var result = _controller.GetApiInfo();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiInfoDto>();

        var apiInfoDto = okResult.Value as ApiInfoDto;
        apiInfoDto!.Service.Should().Be("Exchange Rate Comparison API");
        apiInfoDto.Version.Should().Be("1.0.0");
    }
}