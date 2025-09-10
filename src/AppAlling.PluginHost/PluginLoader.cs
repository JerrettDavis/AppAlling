using System.Reflection;
using AppAlling.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.PluginHost;

/// <summary>
/// Discovers and loads AppAlling plugins from a directory of .NET assemblies.
/// </summary>
/// <param name="pluginDirectory">Directory path to scan for plugin DLLs.</param>
public sealed class PluginLoader(string pluginDirectory)
{
    /// <summary>
    /// Loads plugins found under the directory provided to this loader.
    /// </summary>
    /// <param name="contexts">Outputs the plugin-specific contexts keyed by plugin instance.</param>
    /// <returns>List of successfully created plugin instances.</returns>
    public IReadOnlyList<IAppallingPlugin> LoadPlugins(
        out Dictionary<IAppallingPlugin, IPluginContext> contexts)
    {
        contexts = new();
        if (!Directory.Exists(pluginDirectory))
            return [];

        var pluginsWithDll = Directory.GetFiles(pluginDirectory, "*.dll")
            .Select(TryLoadAssembly)
            .Where(asm => asm is not null)
            .SelectMany(asm => asm!.GetTypes()
                .Where(t => typeof(IAppallingPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => (Type: t, Dll: asm.Location)))
            .Select(pair => (Plugin: TryCreatePlugin(pair.Type), pair.Dll))
            .Where(x => x.Plugin is not null)
            .ToList();

        foreach (var (plugin, dll) in pluginsWithDll)
            contexts[plugin!] = new DefaultPluginContext(Path.GetDirectoryName(dll)!);

        return pluginsWithDll.Select(x => x.Plugin!).ToList();

        static Assembly? TryLoadAssembly(string dll)
        {
            try { return Assembly.LoadFrom(dll); }
            catch { return null; }
        }

        static IAppallingPlugin? TryCreatePlugin(Type t)
        {
            try { return Activator.CreateInstance(t) as IAppallingPlugin; }
            catch { return null; }
        }
    }

    private sealed class DefaultPluginContext(string root) : IPluginContext
    {
        public string RootDirectory => root;
    }

    /// <summary>
    /// Invokes <see cref="IAppallingPlugin.ConfigureServices"/> on all plugins to register their DI services.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="plugins">Plugins to configure.</param>
    /// <param name="ctxs">Contexts keyed by plugin.</param>
    public static void ConfigureAll(
        IServiceCollection services,
        IEnumerable<IAppallingPlugin> plugins,
        Dictionary<IAppallingPlugin, IPluginContext> ctxs)
    {
        foreach (var p in plugins)
            p.ConfigureServices(services, ctxs[p]);
    }
}