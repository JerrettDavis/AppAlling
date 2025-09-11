using System.Runtime.InteropServices;
using System.Windows.Forms;
using AppAlling.Abstractions.State;
using AppAlling.Application.State;
using AppAlling.Tests.Support;
using AppAlling.UI.WinForms;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;
using Timer = System.Windows.Forms.Timer;

namespace AppAlling.Tests;

[Feature("E2E – Program → DI → PluginLoader → MainForm")]
public class E2EProgramToMainFormTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // --- Win32 helpers to auto-dismiss the Hello MessageBox ---
    // ReSharper disable once InconsistentNaming
    private const int WM_CLOSE = 0x0010;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static void AutoDismissMessageBox(string expectedTitle)
    {
        var timer = new Timer { Interval = 10 };
        timer.Tick += (_, _) =>
        {
            var h = FindWindow("#32770", expectedTitle);
            if (h != IntPtr.Zero)
            {
                SendMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }

    // -------- Scenarios --------

    [Scenario("Given in-memory config + real plugin; When I bootstrap and run a message loop; Then menus render and commands execute end-to-end")]
    [StaFact]
    public Task App_bootstraps_and_executes_commands()
        => Given("a temp plugin folder with HelloWorld", () =>
            {
                var dir = TestBootstrap.CreateTempPluginDir();
                TestBootstrap.CopyHelloWorldDll(dir);
                return dir;
            })
            .And("an in-memory configuration pointing to that folder", TestBootstrap.InMemoryConfig)
            .When("I bootstrap provider and MainForm via Bootstrap.Build", cfg => Bootstrap.Build(cfg, pluginDirOverride: cfg["Plugins:Directory"]))
            .And("I run a short Application.Run loop that clicks Tools → Say Hello and then quits", tuple =>
            {
                var (_, form) = tuple;

                // fire a timer to click the menu and then quit
                AutoDismissMessageBox("AppAlling"); // dismiss the Hello box

                var clickTimer = new Timer { Interval = 50 };
                using var ctx = new TestAppContext(form);
                clickTimer.Tick += (_, _) =>
                {
                    clickTimer.Stop();
                    // Navigate to Tools → Say Hello
                    var tools = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().First(i => i.Text?.Replace("&", "") == "Tools");
                    var hello = tools.DropDownItems.OfType<ToolStripMenuItem>().First(i => i.Text?.Replace("&", "") == "Say Hello");
                    hello.PerformClick();

                    // after another short delay, quit the loop
                    var quit = new Timer { Interval = 50 };
                    quit.Tick += (_, _) =>
                    {
                        quit.Stop();
                        quit.Dispose();
                        // ReSharper disable once AccessToDisposedClosure
                        ctx.Quit();
                    };
                    quit.Start();
                };
                clickTimer.Start();

                System.Windows.Forms.Application.Run(ctx);

                return tuple;
            })
            .Then("the store is available in DI (sanity), theme initially Light", tuple =>
            {
                var (sp, _) = tuple;
                var store = sp.GetRequiredService<IStore<AppState>>();
                return store.Current.Theme == "Light";
            })
            .AssertPassed();

    [Scenario("Given the same setup; When I execute Toggle Theme via the menu; Then the store theme becomes Dark")]
    [StaFact]
    public Task ToggleTheme_end_to_end()
        => Given("a temp plugin folder with HelloWorld", () =>
            {
                var dir = TestBootstrap.CreateTempPluginDir();
                TestBootstrap.CopyHelloWorldDll(dir);
                return dir;
            })
            .And("an in-memory configuration pointing to that folder", TestBootstrap.InMemoryConfig)
            .When("I bootstrap provider and MainForm", cfg => Bootstrap.Build(cfg, pluginDirOverride: cfg["Plugins:Directory"]))
            .And("I run a short loop that clicks View → Toggle Theme and then quits", tuple =>
            {
                var (_, form) = tuple;

                var timer = new Timer { Interval = 50 };
                using var ctx = new TestAppContext(form);
                timer.Tick += (_, _) =>
                {
                    timer.Stop();

                    var view = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().First(i => i.Text?.Replace("&", "") == "View");
                    var toggle = view.DropDownItems.OfType<ToolStripMenuItem>().First(i => i.Text?.Replace("&", "") == "Toggle Theme");
                    toggle.PerformClick();

                    var quit = new Timer { Interval = 20 };
                    quit.Tick += (_, _) =>
                    {
                        quit.Stop();
                        quit.Dispose();
                        // ReSharper disable once AccessToDisposedClosure
                        ctx.Quit();
                    };
                    quit.Start();
                };
                timer.Start();

                System.Windows.Forms.Application.Run(ctx);

                return tuple;
            })
            .Then("the theme is now Dark in the store", tuple =>
            {
                var (sp, _) = tuple;
                var store = sp.GetRequiredService<IStore<AppState>>();
                return store.Current.Theme == "Dark";
            })
            .AssertPassed();
}