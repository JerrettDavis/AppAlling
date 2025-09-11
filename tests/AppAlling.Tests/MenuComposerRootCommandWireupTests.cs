using System.Reflection;
using System.Windows.Forms;
using AppAlling.Abstractions;
using AppAlling.UI.WinForms;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("MenuComposer – root command marker & single-click handler wiring")]
public class MenuComposerRootCommandWireupTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Must match MenuComposer's prefix
    private const string RootCmdTagPrefix = "__root_cmd__:";

    // Mutable holder shared across steps so event handlers can mutate it.
    private sealed class Counter
    {
        public int ExecCount;
        public string? LastId;
    }

    // --- helpers ---
    // Host the MenuStrip on a Form so root-item Click handlers are normally valid.
    // (We still invoke OnClick via reflection to avoid a message loop.)
    private static (Form Host, MenuStrip Strip, Counter C) Given_an_empty_menu_and_counter()
    {
        var host = new Form(); // not shown; just to parent the strip
        var strip = new MenuStrip();
        host.MainMenuStrip = strip;
        host.Controls.Add(strip);
        return (host, strip, new Counter());
    }

    private static (Form Host, MenuStrip Strip, Counter C) When_I_merge_root_with_command(
        (Form Host, MenuStrip Strip, Counter C) ctx,
        string title,
        string commandId)
    {
        var roots = new[]
        {
            new MenuItemDescriptor(title, CommandId: commandId, Children: null)
        };

        MenuComposer.MergeInto(
            ctx.Strip,
            roots,
            d => new ToolStripMenuItem(d.Title),
            id =>
            {
                ctx.C.ExecCount++;
                ctx.C.LastId = id;
                return Task.CompletedTask;
            });

        return ctx;
    }

    private static (Form Host, MenuStrip Strip, Counter C) When_I_merge_root_without_command(
        (Form Host, MenuStrip Strip, Counter C) ctx,
        string title)
    {
        var roots = new[]
        {
            new MenuItemDescriptor(title, CommandId: null, Children: null)
        };

        MenuComposer.MergeInto(
            ctx.Strip,
            roots,
            d => new ToolStripMenuItem(d.Title),
            id =>
            {
                ctx.C.ExecCount++;
                ctx.C.LastId = id;
                return Task.CompletedTask;
            });

        return ctx;
    }

    private static ToolStripMenuItem FindTop(MenuStrip strip, string titleNoAmp)
        => strip.Items.OfType<ToolStripMenuItem>()
            .First(i => (i.Text ?? string.Empty).Replace("&", "") == titleNoAmp);

    // Invoke protected OnClick to trigger handlers without a message loop.
    private static (Form Host, MenuStrip Strip, Counter C) When_I_click_top(
        (Form Host, MenuStrip Strip, Counter C) ctx,
        string titleNoAmp)
    {
        var top = FindTop(ctx.Strip, titleNoAmp);
        var mi = typeof(ToolStripItem).GetMethod("OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        mi!.Invoke(top, new object[] { EventArgs.Empty });
        return ctx;
    }

    private static bool Then_top_is_marked_with_command(MenuStrip strip, string titleNoAmp, string id)
    {
        var top = FindTop(strip, titleNoAmp);
        return top.Tag is string tag
               && tag == RootCmdTagPrefix + id
               && tag.StartsWith(RootCmdTagPrefix, StringComparison.Ordinal);
    }

    private static bool Then_top_is_unmarked(MenuStrip strip, string titleNoAmp)
    {
        var top = FindTop(strip, titleNoAmp);
        return top.Tag is not string s || !s.StartsWith(RootCmdTagPrefix, StringComparison.Ordinal);
    }

    private static bool Then_exec_count_is((Form Host, MenuStrip Strip, Counter C) ctx, int expected)
        => ctx.C.ExecCount == expected;

    private static bool Then_last_id_is((Form Host, MenuStrip Strip, Counter C) ctx, string expected)
        => string.Equals(ctx.C.LastId, expected, StringComparison.OrdinalIgnoreCase);

    // --- scenarios ---

    [Scenario("Given a root with CommandId; When merged; Then the top is marked and clicking fires exactly once per click even after a second merge")]
    [StaFact]
    public Task Root_command_is_marked_and_not_double_wired()
        => Given("an empty menu and an exec counter", Given_an_empty_menu_and_counter)
           .When("I merge '&View' with CommandId 'cmd.view.top'", ctx => When_I_merge_root_with_command(ctx, "&View", "cmd.view.top"))
           .Then("the top '&View' is marked with the command id", ctx => Then_top_is_marked_with_command(ctx.Strip, "View", "cmd.view.top"))
           .When("I click the top once", ctx => When_I_click_top(ctx, "View"))
           .Then("exec count is 1", ctx => Then_exec_count_is(ctx, 1))
           .When("I merge the same root again (should not double-wire)", ctx => When_I_merge_root_with_command(ctx, "&View", "cmd.view.top"))
           .When("I click the top again", ctx => When_I_click_top(ctx, "View"))
           .Then("exec count is 2 (not >2)", ctx => Then_exec_count_is(ctx, 2))
           .And("the last executed id is 'cmd.view.top'", ctx => Then_last_id_is(ctx, "cmd.view.top"))
           .AssertPassed();

    [Scenario("Given a root without CommandId; When merged; Then the top is unmarked and clicking does nothing")]
    [StaFact]
    public Task Root_without_command_is_unmarked_and_inert()
        => Given("an empty menu and an exec counter", Given_an_empty_menu_and_counter)
           .When("I merge '&Help' without a CommandId", ctx => When_I_merge_root_without_command(ctx, "&Help"))
           .Then("the top '&Help' is unmarked", ctx => Then_top_is_unmarked(ctx.Strip, "Help"))
           .When("I click the top once", ctx => When_I_click_top(ctx, "Help"))
           .Then("exec count remains 0", ctx => Then_exec_count_is(ctx, 0))
           .AssertPassed();
}