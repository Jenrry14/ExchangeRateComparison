using ExchangeRateComparison.api.Controllers;
using ExchangeRateComparison.api.Dtos;
using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EchangeRateComparison.test.Controllers;

public class ExchangeRateControllerTests
{
    private readonly Mock<IExchangeRateService> _serviceMock;
    private readonly Mock<ILogger<ExchangeRateController>> _loggerMock;
    private readonly ExchangeRateController _controller;

    public ExchangeRateControllerTests()
    {
        _serviceMock = new Mock<IExchangeRateService>();
        _loggerMock = new Mock<ILogger<ExchangeRateController>>();
        _controller = new ExchangeRateController(_serviceMock.Object, _loggerMock.Object);
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
}