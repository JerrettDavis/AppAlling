using System.Reactive.Linq;
using AppAlling.Application.State;
using AppAlling.Application.State.Actions;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("Store – centralized reactive state")]
public class StoreAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ---------- Step helpers ----------
    private static Store<AppState> Given_a_store_with_default_state()
        => new(new AppState(), Reducers.Root);

    private static Store<AppState> When_I_dispatch_SetTheme_Dark(Store<AppState> store)
    {
        store.Dispatch(new SetTheme("Dark"));
        return store;
    }

    private static bool Then_the_store_current_theme_is(Store<AppState> store, string expected)
        => store.Current.Theme == expected;

    private static async Task<Store<AppState>> And_the_store_emits_theme(Store<AppState> store, string expected)
    {
        var emitted = await store.States.Select(s => s.Theme).FirstAsync(t => t == expected);
        // Tiny assert: just ensure awaited value is the expected one
        _ = emitted;
        return store;
    }

    // ---------- Scenarios ----------
    [Scenario("Given a fresh store; When I dispatch SetTheme('Dark'); Then Current is 'Dark' and an event is emitted")]
    [Fact]
    public Task Store_emits_and_updates_current()
        => Given("a store with default state", Given_a_store_with_default_state)
            .When("I dispatch SetTheme('Dark')", When_I_dispatch_SetTheme_Dark)
            .Then("Current.Theme is 'Dark'", st => Then_the_store_current_theme_is(st, "Dark"))
            .And("the store emits 'Dark'", st => And_the_store_emits_theme(st, "Dark"))
            .AssertPassed(TestContext.Current.CancellationToken);
}