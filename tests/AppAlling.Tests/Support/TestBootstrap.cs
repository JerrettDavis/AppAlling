using AppAlling.Plugins.HelloWorld;
using Microsoft.Extensions.Configuration;

namespace AppAlling.Tests.Support;

public static class TestBootstrap
{
    public static IConfiguration InMemoryConfig(string pluginDir)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:Directory"] = pluginDir
            })
            .Build();

    public static string CreateTempPluginDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "appalling_e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void CopyHelloWorldDll(string pluginDir)
    {
        var asmPath = typeof(HelloWorldPlugin).Assembly.Location;
        File.Copy(asmPath, Path.Combine(pluginDir, Path.GetFileName(asmPath)), overwrite: true);
    }
}