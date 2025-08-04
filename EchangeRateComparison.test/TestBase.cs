using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchangeRateComparison.test;

public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; }

    protected TestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }

    protected T GetService<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}