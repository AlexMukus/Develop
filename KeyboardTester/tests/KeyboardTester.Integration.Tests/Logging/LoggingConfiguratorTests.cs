using FluentAssertions;
using KeyboardTester.Infrastructure.Logging;

namespace KeyboardTester.Integration.Tests.Logging;

public class LoggingConfiguratorTests
{
    [Fact]
    public void CreateLoggerFactory_ReturnsWorkingFactory()
    {
        var logFile = Path.Combine(Path.GetTempPath(), $"kt-test-{Guid.NewGuid():N}.log");

        using var factory = LoggingConfigurator.CreateLoggerFactory(logFile);
        var logger = factory.CreateLogger("test");
        logger.LogInformation("Интеграционный тест логирования");

        factory.Should().NotBeNull();
        logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information).Should().BeTrue();
    }
}
