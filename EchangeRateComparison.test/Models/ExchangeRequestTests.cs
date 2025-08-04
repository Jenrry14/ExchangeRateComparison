using ExchangeRateComparison.api.Models.ExchangeRate;
using FluentAssertions;
using Xunit;

namespace ExchangeRateComparison.Tests.Models;

public class ExchangeRequestTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateExchangeRequest()
    {
        // Arrange
        var sourceCurrency = "usd";
        var targetCurrency = "eur";
        var amount = 100.50m;

        // Act
        var request = new ExchangeRequest(sourceCurrency, targetCurrency, amount);

        // Assert
        request.SourceCurrency.Should().Be("USD");
        request.TargetCurrency.Should().Be("EUR");
        request.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(null, "EUR", 100)]
    [InlineData("", "EUR", 100)]
    [InlineData("   ", "EUR", 100)]
    public void Constructor_WithInvalidSourceCurrency_ShouldThrowArgumentException(
        string sourceCurrency, string targetCurrency, decimal amount)
    {
        // Act & Assert
        var act = () => new ExchangeRequest(sourceCurrency, targetCurrency, amount);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("sourceCurrency");
    }

    [Theory]
    [InlineData("USD", null, 100)]
    [InlineData("USD", "", 100)]
    [InlineData("USD", "   ", 100)]
    public void Constructor_WithInvalidTargetCurrency_ShouldThrowArgumentException(
        string sourceCurrency, string targetCurrency, decimal amount)
    {
        // Act & Assert
        var act = () => new ExchangeRequest(sourceCurrency, targetCurrency, amount);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("targetCurrency");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal amount)
    {
        // Act & Assert
        var act = () => new ExchangeRequest("USD", "EUR", amount);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("amount");
    }

    [Fact]
    public void Constructor_WithSameCurrencies_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new ExchangeRequest("USD", "USD", 100);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Source and target currencies must be different");
    }

    [Theory]
    [InlineData("USD", "EUR", 100)]
    [InlineData("eur", "USD", 50.75)]
    [InlineData("GBP", "jpy", 1000)]
    public void Constructor_WithValidInputs_ShouldNormalizeCurrencies(
        string source, string target, decimal amount)
    {
        // Act
        var request = new ExchangeRequest(source, target, amount);

        // Assert
        request.SourceCurrency.Should().Be(source.ToUpper());
        request.TargetCurrency.Should().Be(target.ToUpper());
        request.Amount.Should().Be(amount);
    }
}
