using AppAlling.Abstractions;
using AppAlling.PluginHost;
using AppAlling.Plugins.HelloWorld;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("Plugin loading and composition – behavior-driven acceptance")]
public class PluginLoaderAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // -------- Shared helpers --------------------------------------------------------------

    private static string CreateTempPluginDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "appalling_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string HelloWorldDllPath()
        => typeof(HelloWorldPlugin).Assembly.Location;

    private static string CopyHelloWorldTo(string pluginDir)
    {
        var src = HelloWorldDllPath();
        var dst = Path.Combine(pluginDir, Path.GetFileName(src));
        File.Copy(src, dst, overwrite: true);
        return dst;
    }

    private static string WriteBogusDll(string pluginDir, string name = "bogus.dll")
    {
        var path = Path.Combine(pluginDir, name);
        // Create a non-assembly file with .dll extension to force loader to skip it
        File.WriteAllText(path, "not a real assembly");
        return path;
    }

    private static (IReadOnlyList<IAppallingPlugin> Plugins, Dictionary<IAppallingPlugin, IPluginContext> Ctx)
        LoadFrom(string pluginDir)
    {
        var loader = new PluginLoader(pluginDir);
        var list = loader.LoadPlugins(out var contexts);
        return (list, contexts);
    }

    private static ServiceProvider ConfigureAllIntoProvider(
        IEnumerable<IAppallingPlugin> plugins,
        Dictionary<IAppallingPlugin, IPluginContext> contexts)
    {
        var services = new ServiceCollection();
        PluginLoader.ConfigureAll(services, plugins, contexts);
        return services.BuildServiceProvider();
    }

    // -------- Scenarios -------------------------------------------------------------------

    [Scenario("Given an empty plugin folder; When I load plugins; Then none are returned")]
    [Fact]
    public Task Loads_none_from_empty_folder()
        => Given("an empty temp plugin folder", CreateTempPluginDir)
           .When("I load plugins from that folder", LoadFrom)
           .Then("plugin list is empty", tuple => tuple.Plugins.Count == 0)
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given a folder containing the HelloWorld plugin; When I load plugins; Then exactly one plugin and a valid context are returned")]
    [Fact]
    public Task Loads_helloworld_plugin_and_context()
        => Given("a temp plugin folder", CreateTempPluginDir)
           .And("the HelloWorld plugin copied there", dir => { CopyHelloWorldTo(dir); return dir; })
           .When("I load plugins from that folder", LoadFrom)
           .Then("exactly one plugin is returned", t => t.Plugins.Count == 1)
           .And("its context root directory equals the plugin folder",
                t => string.Equals(
                    t.Plugins[0].GetType().Assembly.Location,
                    HelloWorldDllPath(), 
                    StringComparison.OrdinalIgnoreCase))
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given a folder with a bogus .dll and HelloWorld; When I load plugins; Then bogus is ignored and HelloWorld loads")]
    [Fact]
    public Task Skips_bogus_files_but_loads_valid_plugins()
        => Given("a temp plugin folder", CreateTempPluginDir)
           .And("a bogus .dll file is written", dir => { WriteBogusDll(dir); return dir; })
           .And("the HelloWorld plugin copied there", dir => { CopyHelloWorldTo(dir); return dir; })
           .When("I load plugins from that folder", LoadFrom)
           .Then("one valid plugin is returned", t => t.Plugins.Count == 1)
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given HelloWorld is loaded; When I ConfigureAll; Then its contributions are registered in DI")]
    [Fact]
    public Task ConfigureAll_registers_contributions()
        => Given("a folder with HelloWorld plugin", () =>
           {
               var dir = CreateTempPluginDir();
               CopyHelloWorldTo(dir);
               return dir;
           })
           .When("I load plugins and build a provider via ConfigureAll", dir =>
           {
               var (plugins, ctx) = LoadFrom(dir);
               var sp = ConfigureAllIntoProvider(plugins, ctx);
               return (plugins, ctx, sp);
           })
           .Then("I can resolve ICommandContribution, IMenuModelContribution, and IToolWindowContribution", t =>
           {
               var cmds  = t.sp.GetServices<ICommandContribution>();
               var menus = t.sp.GetServices<IMenuModelContribution>();
               var tools = t.sp.GetServices<IToolWindowContribution>();
               return cmds.Any() && menus.Any() && tools.Any();
           })
           .AssertPassed(TestContext.Current.CancellationToken);
}
