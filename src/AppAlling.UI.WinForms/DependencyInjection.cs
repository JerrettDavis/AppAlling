using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.UI.WinForms;

/// <summary>
/// Dependency injection helpers for the WinForms UI layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Gets the current environment name from ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT.
    /// Defaults to "Development" if not set.
    /// </summary>
    public static string EnvironmentName => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
        "Development";
    
    /// <summary>
    /// Builds the composite configuration (appsettings.json, appsettings.{Environment}.json, env vars, args, user secrets).
    /// </summary>
    public static IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .AddUserSecrets<Program>()
            .Build();
    
    
    /// <summary>
    /// Registers UI services and binds <see cref="AppSettings"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    public static IServiceCollection AddAppAllingUiWinForms(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.Configure<AppSettings>(configuration);
        return services;
    }
}