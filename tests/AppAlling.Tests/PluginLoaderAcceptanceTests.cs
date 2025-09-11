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

    private static string Full(string path) => Path.GetFullPath(path);

    // -------- Scenarios -------------------------------------------------------------------

    [Scenario("Given an empty plugin folder; When I load plugins; Then none are returned")]
    [Fact]
    public Task Loads_none_from_empty_folder()
        => Given("an empty temp plugin folder", CreateTempPluginDir)
           .When("I load plugins from that folder", LoadFrom)
           .Then("plugin list is empty", tuple => tuple.Plugins.Count == 0)
           .And("contexts dictionary is empty", tuple => tuple.Ctx.Count == 0)
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given a folder containing the HelloWorld plugin; When I load plugins; Then exactly one plugin and RootDirectory equals the folder")]
    [Fact]
    public Task Loads_helloworld_plugin_and_valid_rootdirectory()
        => Given("a temp plugin folder", CreateTempPluginDir)
           .And("the HelloWorld plugin copied there", dir => { CopyHelloWorldTo(dir); return dir; })
           .When("I load plugins from that folder", dir =>
           {
               var result = LoadFrom(dir);
               return (Dir: dir, result.Plugins, result.Ctx);
           })
           .Then("exactly one plugin is returned", t => t.Plugins.Count == 1)
           .And("its context RootDirectory equals the temp folder (full path) and exists", t =>
           {
               var plugin = t.Plugins[0];
               var ok = t.Ctx.TryGetValue(plugin, out var ctx) && ctx is not null;
               if (!ok) return false;

               var expected = Full(t.Dir);
               var actual = Full(ctx!.RootDirectory);
               return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
                      && Directory.Exists(actual);
           })
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given a folder with a bogus .dll and HelloWorld; When I load plugins; Then bogus is ignored and RootDirectory is correct")]
    [Fact]
    public Task Skips_bogus_files_but_rootdirectory_is_correct()
        => Given("a temp plugin folder", CreateTempPluginDir)
           .And("a bogus .dll file is written", dir => { WriteBogusDll(dir); return dir; })
           .And("the HelloWorld plugin copied there", dir => { CopyHelloWorldTo(dir); return dir; })
           .When("I load plugins from that folder", dir =>
           {
               var result = LoadFrom(dir);
               return (Dir: dir, result.Plugins, result.Ctx);
           })
           .Then("one valid plugin is returned", t => t.Plugins.Count == 1)
           .And("its RootDirectory equals the temp folder", t =>
           {
               var plugin = t.Plugins[0];
               var ctx = t.Ctx[plugin];
               return string.Equals(Full(ctx.RootDirectory), Full(t.Dir), StringComparison.OrdinalIgnoreCase);
           })
           .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given two copies of HelloWorld in the same folder; When I load; Then all plugin contexts map to the same RootDirectory")]
    [Fact]
    public Task Multiple_plugins_share_same_rootdirectory()
        => Given("a temp plugin folder", CreateTempPluginDir)
           .And("two copies of HelloWorld.dll under different names", dir =>
           {
               var src = HelloWorldDllPath();
               File.Copy(src, Path.Combine(dir, "HelloWorldA.dll"), overwrite: true);
               File.Copy(src, Path.Combine(dir, "HelloWorldB.dll"), overwrite: true);
               return dir;
           })
           .When("I load plugins from that folder", dir =>
           {
               var result = LoadFrom(dir);
               return (Dir: dir, result.Plugins, result.Ctx);
           })
           .Then("at least one plugin instance is loaded", t => t.Plugins.Count >= 1)
           .And("every plugin's RootDirectory equals the temp folder", t =>
           {
               var expected = Full(t.Dir);
               return t.Plugins.All(p => string.Equals(Full(t.Ctx[p].RootDirectory), expected, StringComparison.OrdinalIgnoreCase));
           })
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
