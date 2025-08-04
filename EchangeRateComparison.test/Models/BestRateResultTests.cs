using ExchangeRateComparison.api.Models.ExchangeRate;
using FluentAssertions;
using Xunit;

namespace EchangeRateComparison.test.Models;

public class BestRateResultTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateResult()
    {
        // Arrange
        var bestOffer = new ExchangeResponse("API2", 0.87m, 87m, TimeSpan.FromMilliseconds(200));
        var allResults = new List<ExchangeResponse>
        {
            new("API1", 0.85m, 85m, TimeSpan.FromMilliseconds(150)),
            bestOffer,
            new("API3", 0.86m, 86m, TimeSpan.FromMilliseconds(180))
        };
        var processingTime = TimeSpan.FromMilliseconds(250);

        // Act
        var result = new BestRateResult(bestOffer, allResults, processingTime);

        // Assert
        result.BestOffer.Should().Be(bestOffer);
        result.AllResults.Should().BeEquivalentTo(allResults);
        result.SuccessfulApis.Should().Be(3);
        result.TotalApis.Should().Be(3);
        result.AverageRate.Should().BeApproximately(0.86m, 0.01m);
        result.TotalProcessingTime.Should().Be(processingTime);
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Properties_WithMixedResults_ShouldCalculateCorrectly()
    {
        // Arrange
        var successResponse1 = new ExchangeResponse("API1", 0.85m, 85m, TimeSpan.FromMilliseconds(150));
        var successResponse2 = new ExchangeResponse("API3", 0.86m, 86m, TimeSpan.FromMilliseconds(180));
        var errorResponse = ExchangeResponse.CreateError("API2", "Failed", TimeSpan.FromMilliseconds(5000));
        
        var allResults = new List<ExchangeResponse> { successResponse1, errorResponse, successResponse2 };
        var result = new BestRateResult(successResponse2, allResults, TimeSpan.FromMilliseconds(300));

        // Act & Assert
        result.SuccessfulApis.Should().Be(2);
        result.TotalApis.Should().Be(3);
        result.HasSuccessfulResponse.Should().BeTrue();
        result.SuccessRate.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public void GetSummary_ShouldReturnFormattedString()
    {
        // Arrange
        var bestOffer = new ExchangeResponse("API2", 0.87m, 87m, TimeSpan.FromMilliseconds(200));
        var allResults = new List<ExchangeResponse> { bestOffer };
        var result = new BestRateResult(bestOffer, allResults, TimeSpan.FromMilliseconds(250));

        // Act
        var summary = result.GetSummary();

        // Assert
        summary.Should().Contain("Best offer: API2 (87.00)");
        summary.Should().Contain("Success rate: 1/1 (100.0%)");
        summary.Should().Contain("Processing time: 250ms");
    }
}