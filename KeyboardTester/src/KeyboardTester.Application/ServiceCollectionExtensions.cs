using KeyboardTester.Application.Services;
using KeyboardTester.Application.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KeyboardTester.Application;

/// <summary>
/// Расширения для регистрации сервисов Application Layer в DI-контейнере.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует сервисы и ViewModels Application Layer.
    /// </summary>
    public static IServiceCollection AddKeyboardTesterApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TestSessionService>();
        services.AddSingleton<KeyboardDetectionService>();
        services.AddSingleton<MainViewModel>();

        return services;
    }
}
