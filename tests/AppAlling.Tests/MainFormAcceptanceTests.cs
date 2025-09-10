using System.Drawing;
using System.Windows.Forms;
using AppAlling.Application.State;
using AppAlling.Application.State.Actions;
using AppAlling.Tests.Support;
using AppAlling.UI.WinForms;
using AppAlling.UI.WinForms.Commands;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("MainForm – data-driven UI & reactive behavior")]
public class MainFormAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ----------------- shared helpers -----------------

    private static (ServiceProvider SP, RecordingCommandBus Bus, Store<AppState> Store, MainForm Form)
        Given_mainform_with_demo_contributions()
    {
        // DI with command exec + tool window factory
        var services = new ServiceCollection()
            .AddSingleton<ICommandExec, HelloExec>()
            .AddSingleton<IToolWindowFactory, InspectorFactory>();

        var sp = services.BuildServiceProvider();

        var bus = new RecordingCommandBus();
        var store = new Store<AppState>(new AppState(), Reducers.Root);

        // contributions (normally supplied by plugins)
        var cmd = new DemoCommandContribution();
        var menu = new DemoMenuContribution();
        var tools = new DemoToolWindowContribution();

        // construct MainForm (do NOT show; handle creation is deferred until needed)
        var form = new MainForm(
            sp,      // IServiceProvider
            bus,     // ICommandBus
            [cmd],   // commands
            [menu],  // menus
            [tools], // tools
            store    // store
        );

        return (sp, bus, store, form);
    }

    private static (ServiceProvider SP, RecordingCommandBus Bus, Store<AppState> Store, MainForm Form)
        When_I_click_menu_item(
            (ServiceProvider SP, RecordingCommandBus Bus, Store<AppState> Store, MainForm Form) ctx,
            string topTitleWithoutAmpersand,
            string childTitle)
    {
        var form = ctx.Form;

        // Ensure the control hierarchy is ready (no message loop needed)
        if (!form.IsHandleCreated) form.CreateControl();

        var top = form.MainMenuStrip!.Items
            .OfType<ToolStripMenuItem>()
            .First(i => i.Text?.Replace("&", "") == topTitleWithoutAmpersand);

        var child = top.DropDownItems
            .OfType<ToolStripMenuItem>()
            .First(i => i.Text?.Replace("&", "") == childTitle.Replace("&", ""));

        // Simulate user click
        child.PerformClick();

        return ctx;
    }

    private static bool Then_bus_executed(RecordingCommandBus bus, string expectedId)
        => bus.ExecutedIds.Contains(expectedId, StringComparer.OrdinalIgnoreCase);

    private static (ServiceProvider SP, RecordingCommandBus Bus, Store<AppState> Store, MainForm Form)
        When_store_theme_becomes_dark((ServiceProvider SP, RecordingCommandBus Bus, Store<AppState> Store, MainForm Form) ctx)
    {
        ctx.Store.Dispatch(new SetTheme("Dark"));
        return ctx;
    }

    private static bool Then_form_theme_is_dark(MainForm form)
        => form.BackColor == Color.FromArgb(32, 32, 32) &&
           form.ForeColor == Color.WhiteSmoke &&
           form.Controls.OfType<StatusStrip>().First().Items.OfType<ToolStripStatusLabel>().First().Text == "Theme: Dark";

    private static bool Then_implicit_tool_command_is_registered(RecordingCommandBus bus)
        => bus.List.Keys.Contains("tool.inspector.open", StringComparer.OrdinalIgnoreCase) &&
           string.Equals(bus.List["tool.inspector.open"].Title, "Open Inspector", StringComparison.Ordinal);

    // ----------------- scenarios -----------------

    [Scenario("Given MainForm with demo contributions; When I click Tools → Say Hello; Then the bus executes 'demo.hello'")]
    [StaFact]
    public Task Clicking_menu_executes_command()
        => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
            .When("I click Tools → Say Hello", ctx => When_I_click_menu_item(ctx, "Tools", "Say Hello"))
            .Then("the command bus executed 'demo.hello'", ctx => Then_bus_executed(ctx.Bus, "demo.hello"))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given MainForm with a tool window contribution; When constructed; Then an implicit 'Open Inspector' command is registered")]
    [StaFact]
    public Task Tool_window_registers_implicit_open_command()
        => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
            .Then("the implicit tool open command is registered", ctx => Then_implicit_tool_command_is_registered(ctx.Bus))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given MainForm bound to the store; When I set the theme to Dark; Then the form applies the Dark theme")]
    [StaFact]
    public Task Theme_reacts_to_store_state()
        => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
            .When("the store theme becomes Dark", When_store_theme_becomes_dark)
            .Then("the form shows Dark theme & status text", ctx => Then_form_theme_is_dark(ctx.Form))
            .AssertPassed(TestContext.Current.CancellationToken);
}