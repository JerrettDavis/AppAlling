using System.Reflection;
using System.Runtime.Loader;
using AppAlling.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.PluginHost;

/// <summary>
/// Discovers and loads AppAlling plugins from a directory of .NET assemblies.
/// </summary>
/// <param name="pluginDirectory">Directory path to scan for plugin DLLs.</param>
public sealed class PluginLoader(string pluginDirectory)
{
    // Keep ALCs alive for the lifetime of the loader so assemblies aren't collected.
    private readonly List<AssemblyLoadContext> _alcs = [];

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
            .Select(TryLoadAssemblyWithAlc)
            .Where(asm => asm is not null)
            .SelectMany(asm => asm!
                .GetTypes()
                .Where(t => typeof(IAppallingPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => (Type: t, Dll: asm.Location)))
            .Select(pair => (Plugin: TryCreatePlugin(pair.Type), pair.Dll))
            .Where(x => x.Plugin is not null)
            .ToList();

        foreach (var (plugin, dll) in pluginsWithDll)
        {
            var root = Path.GetFullPath(Path.GetDirectoryName(dll)!);
            contexts[plugin!] = new DefaultPluginContext(root);
        }

        return pluginsWithDll.Select(x => x.Plugin!).ToList();

        Assembly? TryLoadAssemblyWithAlc(string dll)
        {
            try
            {
                var root = Path.GetFullPath(Path.GetDirectoryName(dll)!);
                var alc = new PluginAlc(root);
                _alcs.Add(alc); // keep alive
                return alc.LoadFromAssemblyPath(Path.GetFullPath(dll));
            }
            catch
            {
                return null;
            }
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

    private sealed class PluginAlc : AssemblyLoadContext
    {
        private readonly string _dir;
        public PluginAlc(string dir) : base(isCollectible: false) => _dir = dir;

        // Resolve dependency assemblies from the same plugin directory
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var candidate = Path.Combine(_dir, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }
}
