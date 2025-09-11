# Unit Tests

Unit tests validate **individual components** in isolation.  
They are the fastest layer of the testing pyramid and focus on **logic correctness**, not wiring or integration.

---

## Purpose

Unit tests ensure that:
- **Command handlers** increment counters or mutate state correctly.
- **Reducers** in the state store produce correct new state.
- **Helper functions** (e.g., `ShortcutParser`, `MenuComposer.NormalizeTitle`) behave as expected.
- **Internal logic** (like `CommandRegistry.ResolveToolWindowFactory`) works for edge cases.

Unlike [acceptance tests](acceptance-tests.md), unit tests do **not** exercise the full DI container or UI.

---

## Example: CommandBus

```csharp
using AppAlling.Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

[Feature("Command bus – unit tests")]
public class CommandBusTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed class Counter { public int Value; }

    [Scenario("Given a bus with a registered command; When executed once; Then the counter increments to 1")]
    [Fact]
    public Task Service_is_incremented_via_DI()
        => Given("services with a counter service and a command bus", () =>
        {
            var services = new ServiceCollection().AddSingleton<Counter>();
            var sp = services.BuildServiceProvider();

            var bus = new CommandBus();
            bus.Register(new CommandDescriptor("demo.increment", "Increment"), p =>
            {
                p.GetRequiredService<Counter>().Value++;
                return Task.CompletedTask;
            });

            return (bus, sp);
        })
        .When("I execute 'demo.increment' once", async ctx =>
        {
            await ctx.bus.ExecuteAsync("demo.increment", ctx.sp);
            return ctx;
        })
        .Then("counter == 1", ctx => ctx.sp.GetRequiredService<Counter>().Value == 1)
        .AssertPassed();
}
```

---

## Example: Reducer Logic

```csharp
[Scenario("Given AppState with Light theme; When SetTheme(Dark) dispatched; Then state has Dark theme")]
[Fact]
public Task Theme_reducer_changes_state()
    => Given("an AppState with theme Light", () => new AppState { Theme = "Light" })
       .When("I dispatch SetTheme(\"Dark\")", state =>
       {
           return Reducers.Root(state, new SetTheme("Dark"));
       })
       .Then("the new state's theme is Dark", s => s.Theme == "Dark")
       .AssertPassed();
```

---

## Example: MenuComposer Helpers

```csharp
[Scenario("NormalizeTitle removes ampersands")]
[Fact]
public void NormalizeTitle_strips_ampersands()
{
    var normalized = typeof(MenuComposer)
        .GetMethod("NormalizeTitle", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object[] { "&File" });

    Assert.Equal("File", normalized);
}
```

---

## Tips for Unit Tests

* **Run them in parallel:** Since they have no UI or global state, they are safe to parallelize.
* **Use xUnit + TinyBDD for fluent Gherkin style**, or regular xUnit `Fact`/`Theory` for pure logic.
* **Prefer deterministic, side-effect-free tests** (avoid hitting file system or network).
* **Cover edge cases:** e.g., empty command id, null factory, duplicate registration (last write wins).

---

## See Also

* [Acceptance Tests](acceptance-tests.md)
* [Architecture Overview](../architecture/overview.md)
* [Command Bus](../architecture/command-bus.md)

