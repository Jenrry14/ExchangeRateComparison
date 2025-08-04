using ExchangeRateComparison.api.Models.ExchangeRate;
using ExchangeRateComparison.api.Services;
using ExchangeRateComparison.api.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EchangeRateComparison.test.Services;

public class ExchangeRateServiceTests : TestBase
{
    private readonly Mock<ILogger<ExchangeRateService>> _loggerMock;
    private readonly Mock<IExchangeRateClient> _client1Mock;
    private readonly Mock<IExchangeRateClient> _client2Mock;
    private readonly Mock<IExchangeRateClient> _client3Mock;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExchangeRateService>>();
        _client1Mock = new Mock<IExchangeRateClient>();
        _client2Mock = new Mock<IExchangeRateClient>();
        _client3Mock = new Mock<IExchangeRateClient>();

        SetupMockClients();

        var clients = new[] { _client1Mock.Object, _client2Mock.Object, _client3Mock.Object };
        _service = new ExchangeRateService(clients, _loggerMock.Object);
    }

    private void SetupMockClients()
    {
        _client1Mock.Setup(x => x.ApiName).Returns("API1");
        _client1Mock.Setup(x => x.IsEnabled).Returns(true);
        
        _client2Mock.Setup(x => x.ApiName).Returns("API2");
        _client2Mock.Setup(x => x.IsEnabled).Returns(true);
        
        _client3Mock.Setup(x => x.ApiName).Returns("API3");
        _client3Mock.Setup(x => x.IsEnabled).Returns(true);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WithAllSuccessfulResponses_ShouldReturnBestRate()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 100);
        
        var response1 = new ExchangeResponse("API1", 0.85m, 85m, TimeSpan.FromMilliseconds(200));
        var response2 = new ExchangeResponse("API2", 0.87m, 87m, TimeSpan.FromMilliseconds(300));
        var response3 = new ExchangeResponse("API3", 0.86m, 86m, TimeSpan.FromMilliseconds(250));

        _client1Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response1);
        _client2Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response2);
        _client3Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response3);

        // Act
        var result = await _service.GetBestExchangeRateAsync(request);

        // Assert
        result.BestOffer.Should().Be(response2); // Highest converted amount (87)
        result.AllResults.Should().HaveCount(3);
        result.SuccessfulApis.Should().Be(3);
        result.TotalApis.Should().Be(3);
        result.AverageRate.Should().BeApproximately(0.86m, 0.01m);
        result.SuccessRate.Should().Be(100);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WithSomeFailedResponses_ShouldReturnBestFromSuccessful()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 100);
        
        var successResponse1 = new ExchangeResponse("API1", 0.85m, 85m, TimeSpan.FromMilliseconds(200));
        var errorResponse = ExchangeResponse.CreateError("API2", "Connection failed", TimeSpan.FromMilliseconds(5000));
        var successResponse3 = new ExchangeResponse("API3", 0.86m, 86m, TimeSpan.FromMilliseconds(250));

        _client1Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(successResponse1);
        _client2Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(errorResponse);
        _client3Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(successResponse3);

        // Act
        var result = await _service.GetBestExchangeRateAsync(request);

        // Assert
        result.BestOffer.Should().Be(successResponse3); // Highest from successful (86)
        result.AllResults.Should().HaveCount(3);
        result.SuccessfulApis.Should().Be(2);
        result.TotalApis.Should().Be(3);
        result.SuccessRate.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WithAllFailedResponses_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 100);
        
        var errorResponse1 = ExchangeResponse.CreateError("API1", "Connection failed", TimeSpan.FromMilliseconds(5000));
        var errorResponse2 = ExchangeResponse.CreateError("API2", "Timeout", TimeSpan.FromMilliseconds(5000));
        var errorResponse3 = ExchangeResponse.CreateError("API3", "Service unavailable", TimeSpan.FromMilliseconds(1000));

        _client1Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(errorResponse1);
        _client2Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(errorResponse2);
        _client3Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(errorResponse3);

        // Act & Assert
        var act = async () => await _service.GetBestExchangeRateAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*All APIs failed to provide exchange rates*");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WithNoEnabledClients_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _client1Mock.Setup(x => x.IsEnabled).Returns(false);
        _client2Mock.Setup(x => x.IsEnabled).Returns(false);
        _client3Mock.Setup(x => x.IsEnabled).Returns(false);

        var request = new ExchangeRequest("USD", "EUR", 100);

        // Act & Assert
        var act = async () => await _service.GetBestExchangeRateAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("No exchange rate APIs are enabled");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_ShouldSelectHighestConvertedAmount()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 100);
        
        var response1 = new ExchangeResponse("API1", 0.80m, 80m, TimeSpan.FromMilliseconds(200));   // Lowest
        var response2 = new ExchangeResponse("API2", 0.90m, 90m, TimeSpan.FromMilliseconds(300));   // Highest
        var response3 = new ExchangeResponse("API3", 0.85m, 85m, TimeSpan.FromMilliseconds(250));   // Middle

        _client1Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response1);
        _client2Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response2);
        _client3Mock.Setup(x => x.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(response3);

        // Act
        var result = await _service.GetBestExchangeRateAsync(request);

        // Assert
        result.BestOffer.Should().Be(response2);
        result.BestOffer.ConvertedAmount.Should().Be(90m);
        result.BestOffer.ApiName.Should().Be("API2");
    }

    [Fact]
    public async Task CheckApiHealthAsync_ShouldReturnHealthStatusForAllApis()
    {
        // Arrange
        _client1Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _client2Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _client3Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.CheckApiHealthAsync();

        // Assert
        result.Should().HaveCount(3);
        result["API1"].Should().BeTrue();
        result["API2"].Should().BeFalse();
        result["API3"].Should().BeTrue();
    }

    [Fact]
    public async Task CheckApiHealthAsync_WhenExceptionThrown_ShouldReturnFalseForThatApi()
    {
        // Arrange
        _client1Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _client2Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Connection failed"));
        _client3Mock.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.CheckApiHealthAsync();

        // Assert
        result.Should().HaveCount(3);
        result["API1"].Should().BeTrue();
        result["API2"].Should().BeFalse(); // Should be false due to exception
        result["API3"].Should().BeTrue();
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCurrentStatistics()
    {
        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().BeGreaterOrEqualTo(0);
        result.ApiStats.Should().NotBeNull();
    }

    [Fact]
    public void ResetStatistics_ShouldResetStatisticsData()
    {
        // Act & Assert (Should not throw)
        _service.ResetStatistics();
        
        // Verify through logging mock if needed
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("statistics have been reset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
