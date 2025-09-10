using AppAlling.Application.State;
using AppAlling.Application.State.Actions;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("App state – reducers")]
public class StateAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ---------- Step helpers ----------
    private static AppState Given_an_initial_state_Light() => new(Theme: "Light", OpenToolWindowCount: 0);

    private static AppState When_the_reducer_applies_SetTheme_Dark(AppState s)
        => Reducers.Root(s, new SetTheme("Dark"));

    private static bool Then_the_theme_should_equal(AppState s, string expected) => s.Theme == expected;

    private static AppState When_a_tool_window_is_opened(AppState s)
        => Reducers.Root(s, new ToolWindowOpened());

    private static AppState When_a_tool_window_is_closed(AppState s)
        => Reducers.Root(s, new ToolWindowClosed());

    private static bool Then_the_open_count_should_equal(AppState s, int expected)
        => s.OpenToolWindowCount == expected;

    // ---------- Scenarios ----------
    [Scenario("Given a Light theme; When SetTheme('Dark') is reduced; Then theme becomes Dark")]
    [Fact]
    public Task Theme_is_set_to_Dark()
        => Given("an initial Light state", Given_an_initial_state_Light)
            .When("the reducer applies SetTheme('Dark')", When_the_reducer_applies_SetTheme_Dark)
            .Then("the theme should equal 'Dark'", s => Then_the_theme_should_equal(s, "Dark"))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given no open tool windows; When one is opened then closed; Then count returns to 0")]
    [Fact]
    public Task Open_close_tool_window_cycles_count()
        => Given("an initial Light state", Given_an_initial_state_Light)
            .When("a tool window is opened", When_a_tool_window_is_opened)
            .And("a tool window is closed", When_a_tool_window_is_closed)
            .Then("the open count should equal 0", s => Then_the_open_count_should_equal(s, 0))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given no open tool windows; When two are opened; Then count == 2")]
    [Fact]
    public Task Open_two_tool_windows_increments()
        => Given("an initial Light state", Given_an_initial_state_Light)
            .When("first window opened", When_a_tool_window_is_opened)
            .And("second window opened", When_a_tool_window_is_opened)
            .Then("the open count should equal 2", s => Then_the_open_count_should_equal(s, 2))
            .AssertPassed(TestContext.Current.CancellationToken);
}