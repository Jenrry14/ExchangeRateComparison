namespace ExchangeRateComparison.api.Services.Interfaces;

public interface IDynamicCredentialsService
{
    Task ConfigureApiCredentialsAsync(string? api1Key, string? api2Key, string? api3Key);
}