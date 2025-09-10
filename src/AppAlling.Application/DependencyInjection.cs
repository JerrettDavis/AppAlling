using AppAlling.Abstractions.State;
using AppAlling.Application.Commands;
using AppAlling.Application.State;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.Application;

/// <summary>
/// Dependency injection extensions for the core AppAlling application services
/// (state store and command bus).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the AppAlling application services into the provided service collection.
    /// </summary>
    /// <param name="services">The DI container to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAppAllingApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IStore<AppState>>(_ => new Store<AppState>(new AppState(), Reducers.Root));
        services.AddSingleton<ICommandBus, CommandBus>();
        return services;
    }
}