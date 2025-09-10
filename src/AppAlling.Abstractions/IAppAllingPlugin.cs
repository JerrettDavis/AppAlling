namespace AppAlling.Abstractions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contract that every AppAlling plugin must implement. A plugin can contribute
/// commands, menus, tool windows and register services via dependency injection.
/// </summary>
public interface IAppallingPlugin
{
    /// <summary>
    /// Gets basic metadata that identifies the plugin in the host (id, name, version, author).
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Allows the plugin to register its services and contribution surfaces into the host container.
    /// </summary>
    /// <param name="services">The DI service collection to add registrations to.</param>
    /// <param name="context">The plugin context, including the plugin root directory.</param>
    void ConfigureServices(IServiceCollection services, IPluginContext context);
}

/// <summary>
/// Immutable metadata describing a plugin.
/// </summary>
/// <param name="Id">A unique, stable id (e.g., "vendor.product").</param>
/// <param name="Name">Human-friendly plugin name.</param>
/// <param name="Version">Semantic version string of this plugin build.</param>
/// <param name="Author">Optional author or organization.</param>
public sealed record PluginMetadata(string Id, string Name, string Version, string? Author = null);

/// <summary>
/// Contextual information passed to plugins during configuration.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Gets the absolute path to the plugin's root directory on disk.
    /// Use this to resolve content files, settings, etc.
    /// </summary>
    string RootDirectory { get; }      
}
