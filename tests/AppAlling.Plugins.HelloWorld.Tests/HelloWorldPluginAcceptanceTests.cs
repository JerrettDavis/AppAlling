using System.Runtime.InteropServices;
using AppAlling.Abstractions;
using AppAlling.Abstractions.State;
using AppAlling.Application.State;
using AppAlling.UI.WinForms.Commands;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;
using AppState = AppAlling.Application.State.AppState;
using Reducers = AppAlling.Application.State.Reducers;

namespace AppAlling.Plugins.HelloWorld.Tests;

[Feature("HelloWorldPlugin – contributions, metadata, and behavior")]
public class HelloWorldPluginAcceptanceTests(ITestOutputHelper output) :
    TinyBddXunitBase(output)
{
    // ---------------- helpers / Given steps ----------------

    private sealed class DummyPluginContext : IPluginContext
    {
        public string RootDirectory { get; init; } = Path.GetTempPath();
    }

    private static (ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store)
        Given_services_with_store_and_configured_helloworld_plugin()
    {
        var services = new ServiceCollection();

        // Shared app store used by plugin execs (view.toggleTheme)
        var store = new Store<AppState>(new AppState(), Reducers.Root);
        services.AddSingleton<IStore<AppState>>(store);

        var plugin = new HelloWorldPlugin();
        plugin.ConfigureServices(services, new DummyPluginContext());

        var sp = services.BuildServiceProvider();
        return (sp, plugin, store);
    }

    // ---------------- When steps ----------------

    private static (ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store,
        IEnumerable<ICommandContribution> Cmds,
        IEnumerable<IMenuModelContribution> Menus,
        IEnumerable<IToolWindowContribution> Tools)
        When_I_resolve_contributions((ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store) ctx)
    {
        var cmds = ctx.SP.GetServices<ICommandContribution>();
        var menus = ctx.SP.GetServices<IMenuModelContribution>();
        var tools = ctx.SP.GetServices<IToolWindowContribution>();
        return (ctx.SP, ctx.Plugin, ctx.Store, cmds, menus, tools);
    }

    private static (ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store)
        When_I_execute_view_toggleTheme((ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store) ctx)
    {
        // Resolve all ICommandExec and run the one that handles "view.toggleTheme"
        var execs = ctx.SP.GetServices<ICommandExec>();
        var handler = execs.FirstOrDefault(e => e.CanHandle("view.toggleTheme"));
        Assert.NotNull(handler); // safety

        handler.ExecuteAsync(ctx.SP).GetAwaiter().GetResult();
        return ctx;
    }

    // ---------------- Then steps ----------------

    private static bool Then_contributions_are_registered(
        (ServiceProvider SP, HelloWorldPlugin Plugin, Store<AppState> Store,
            IEnumerable<ICommandContribution> Cmds,
            IEnumerable<IMenuModelContribution> Menus,
            IEnumerable<IToolWindowContribution> Tools) ctx)
        => ctx.Cmds.Any() && ctx.Menus.Any() && ctx.Tools.Any();

    private static bool Then_command_metadata_is_correct(HelloWorldPlugin plugin)
    {
        var list = plugin.DescribeCommands().ToArray();
        var ids = list.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // presence
        if (!ids.SetEquals(["tools.sayHello", "view.toggleTheme", "tool.hello.open"])) return false;

        // shortcut on toggleTheme
        var toggle = list.First(c => c.Id.Equals("view.toggleTheme", StringComparison.OrdinalIgnoreCase));
        return string.Equals(toggle.Shortcut, "Ctrl+T", StringComparison.Ordinal);
    }

    private static bool Then_menu_model_contains_expected_items(HelloWorldPlugin plugin)
    {
        var roots = plugin.BuildMenuModel().ToArray();

        var tools = roots.FirstOrDefault(r => r.Title.Replace("&", "") == "Tools");
        var view = roots.FirstOrDefault(r => r.Title.Replace("&", "") == "View");

        if (tools is null || view is null) return false;

        var toolsOk = tools.Children?
            .Any(c => c.Title.Replace("&", "") == "Say Hello"
                      && c.CommandId == "tools.sayHello") == true;

        var viewOk = view.Children?
                         .Any(c => c.Title.Replace("&", "") == "Toggle Theme"
                                   && c.CommandId == "view.toggleTheme") == true
                     && view.Children?
                         .Any(c => c.Title.Replace("&", "") == "Hello Window"
                                   && c.CommandId == "tool.hello.open") == true;

        return toolsOk && viewOk;
    }

    private static bool Then_tool_descriptor_and_factory_are_valid(ServiceProvider sp, HelloWorldPlugin plugin)
    {
        var desc = plugin.DescribeToolWindows().Single();
        if (desc.Title != "Hello Window" || desc.CommandId != "tool.hello.open") return false;

        var factory = sp.GetServices<IToolWindowFactory>()
            .FirstOrDefault(f => f.CommandId.Equals(desc.CommandId, StringComparison.OrdinalIgnoreCase));
        if (factory is null) return false;

        using var form = factory.Create();
        return form is HelloForm && form.Text == "Hello Window";
    }

    private static bool Then_theme_toggled(Store<AppState> store, string expected)
        => store.Current.Theme == expected;

    // ---------------- Scenarios ----------------

    [Scenario("Given services with the store and HelloWorld configured; When I resolve contributions; Then ICommand/IMenu/IToolWindow are available")]
    [Fact]
    public Task Contributions_are_registered_in_DI()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .When("I resolve contributions", When_I_resolve_contributions)
            .Then("all contribution interfaces are present", Then_contributions_are_registered)
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given the HelloWorld plugin; When I inspect DescribeCommands; Then it includes ids and a Ctrl+T shortcut for view.toggleTheme")]
    [Fact]
    public Task Command_metadata_matches_contract()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .Then("command metadata is correct", ctx => Then_command_metadata_is_correct(ctx.Plugin))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given the HelloWorld plugin; When I inspect its menu model; Then Tools/Say Hello and View/Toggle Theme + Hello Window exist")]
    [Fact]
    public Task Menu_model_contains_expected_items()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .Then("menu model has expected roots and children", ctx => Then_menu_model_contains_expected_items(ctx.Plugin))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given HelloWorld is configured; When I locate the tool window factory; Then it creates a HelloForm with the right title")]
    [StaFact] // STA because we new up a Form
    public Task Tool_window_descriptor_and_factory_are_valid()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .Then("tool descriptor + factory are valid", ctx => Then_tool_descriptor_and_factory_are_valid(ctx.SP, ctx.Plugin))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given store theme is Light; When I execute 'view.toggleTheme'; Then the theme becomes Dark")]
    [Fact]
    public Task ToggleTheme_executes_against_store()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .When("I execute the view.toggleTheme handler", When_I_execute_view_toggleTheme)
            .Then("the store theme is 'Dark'", ctx => Then_theme_toggled(ctx.Store, "Dark"))
            .AssertPassed(TestContext.Current.CancellationToken);
    
    
    [Scenario("Given the HelloWorld plugin; When I read Metadata; Then Id=hello.world, Name=Hello World, Version=1.1.0")]
    [Fact]
    public Task Metadata_is_correct()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .Then("plugin metadata matches expected values", ctx =>
            {
                var meta = ctx.Plugin.Metadata;
                return meta.Id == "hello.world"
                       && meta.Name == "Hello World"
                       && meta.Version == "1.1.0";
            })
            .AssertPassed(TestContext.Current.CancellationToken);
    
    
    [Scenario("Given HelloWorld is configured; When I execute 'tools.sayHello'; Then a message box is shown and dismissed")]
    [StaFact] // STA is required for WinForms + MessageBox
    public Task SayHello_shows_a_message_box()
        => Given("services + store + configured plugin", Given_services_with_store_and_configured_helloworld_plugin)
            .When("I execute the tools.sayHello handler with an auto-dismiss timer", ctx =>
            {
                // Arrange: start a short-interval timer to find and close the message box.
                var dismissed = 0;
                using var timer = new System.Windows.Forms.Timer();
                timer.Interval = 10;
                timer.Tick += (_, __) =>
                {
                    var hWnd = FindWindow("#32770", "AppAlling"); // MessageBox class + title
                    if (hWnd == IntPtr.Zero)
                        return;
                    
                    SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    dismissed++;
                    // ReSharper disable once AccessToDisposedClosure
                    timer.Stop();
                };
                timer.Start();

                // Act: run the sayHello command (registered by the plugin)
                var execs = ctx.SP.GetServices<ICommandExec>();
                var hello = execs.FirstOrDefault(e => e.CanHandle("tools.sayHello"));
                Assert.NotNull(hello); // guard
                hello!.ExecuteAsync(ctx.SP).GetAwaiter().GetResult();

                // Assert state for this step (we'll also assert in Then)
                return (ctx.SP, ctx.Plugin, ctx.Store, Dismissed: dismissed);
            })
            .Then("the message box was dismissed exactly once", t => t.Dismissed == 1)
            .AssertPassed();

    #region Win32 helpers
    private const int WM_CLOSE = 0x0010;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    #endregion
}