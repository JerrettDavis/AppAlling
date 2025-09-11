using AppAlling.Application;
using AppAlling.PluginHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.UI.WinForms;

public static class Bootstrap
{
    public static (ServiceProvider Provider, MainForm Form) Build(
        IConfiguration configuration,
        string? pluginDirOverride = null)
    {
        var services = new ServiceCollection()
            .AddAppAllingApplication()
            .AddAppAllingUiWinForms(configuration)
            .AddTransient<MainForm>();

        var appSettings = configuration.Get<AppSettings>();
        var pluginDir = pluginDirOverride ?? appSettings?.Plugins.Directory ?? "Plugins";

        var loader = new PluginLoader(pluginDir);
        var plugins = loader.LoadPlugins(out var contexts);
        PluginLoader.ConfigureAll(services, plugins, contexts);

        var provider = services.BuildServiceProvider();
        var form = provider.GetRequiredService<MainForm>();
        return ((ServiceProvider)provider, form);
    }
}