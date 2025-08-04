using ExchangeRateComparison.api.Models.ExchangeRate;
using FluentAssertions;
using Xunit;

namespace EchangeRateComparison.test.Models;

public class ExchangeResponseTests
{
    [Fact]
    public void Constructor_WithSuccessfulResponse_ShouldCreateValidExchangeResponse()
    {
        // Arrange
        var apiName = "TestAPI";
        var rate = 1.25m;
        var convertedAmount = 125m;
        var responseTime = TimeSpan.FromMilliseconds(300);

        // Act
        var response = new ExchangeResponse(apiName, rate, convertedAmount, responseTime);

        // Assert
        response.ApiName.Should().Be(apiName);
        response.Rate.Should().Be(rate);
        response.ConvertedAmount.Should().Be(convertedAmount);
        response.ResponseTime.Should().Be(responseTime);
        response.Success.Should().BeTrue();
        response.Error.Should().BeNull();
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateError_ShouldCreateErrorResponse()
    {
        // Arrange
        var apiName = "TestAPI";
        var error = "Connection timeout";
        var responseTime = TimeSpan.FromMilliseconds(5000);

        // Act
        var response = ExchangeResponse.CreateError(apiName, error, responseTime);

        // Assert
        response.ApiName.Should().Be(apiName);
        response.Rate.Should().BeNull();
        response.ConvertedAmount.Should().BeNull();
        response.ResponseTime.Should().Be(responseTime);
        response.Success.Should().BeFalse();
        response.Error.Should().Be(error);
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateTimeout_ShouldCreateTimeoutResponse()
    {
        // Arrange
        var apiName = "TestAPI";
        var responseTime = TimeSpan.FromMilliseconds(5000);

        // Act
        var response = ExchangeResponse.CreateTimeout(apiName, responseTime);

        // Assert
        response.ApiName.Should().Be(apiName);
        response.Success.Should().BeFalse();
        response.Error.Should().Be("Request timeout");
        response.ResponseTime.Should().Be(responseTime);
    }

    [Fact]
    public void CreateUnavailable_ShouldCreateUnavailableResponse()
    {
        // Arrange
        var apiName = "TestAPI";
        var responseTime = TimeSpan.FromMilliseconds(1000);

        // Act
        var response = ExchangeResponse.CreateUnavailable(apiName, responseTime);

        // Assert
        response.ApiName.Should().Be(apiName);
        response.Success.Should().BeFalse();
        response.Error.Should().Be("API temporarily unavailable");
        response.ResponseTime.Should().Be(responseTime);
    }
}
