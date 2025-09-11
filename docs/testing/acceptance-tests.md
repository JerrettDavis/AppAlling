# Acceptance Tests

Acceptance tests validate that the **entire application** works as expected from the perspective of an end user or consuming system.  
They are written using [TinyBDD](https://github.com/JerrettDavis/TinyBDD) and use **Gherkin-style scenarios** with `Given/When/Then` steps.

---

## Purpose

Acceptance tests give you confidence that:
- **DI wiring** is correct (services are properly registered).
- **Plugins** are loaded, menus built, and commands wired up.
- **UI and state store** respond correctly to actions.
- **User flows** behave as expected — e.g., clicking a menu triggers a command.

They sit at the **highest level** of the testing pyramid, often exercising:
- `Program.cs` entry point
- `PluginLoader`
- `MainForm` initialization and event wiring
- Command bus execution
- Reactive store updates

---

## Example: Menu Interaction

```csharp
[Feature("MainForm – data-driven UI & reactive behavior")]
public class MainFormAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Given MainForm with demo contributions; When I click Tools → Say Hello; Then the bus executes 'demo.hello'")]
    [StaFact]
    public Task Clicking_menu_executes_command()
        => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
           .When("I click Tools → Say Hello", ctx => When_I_click_menu_item(ctx.Form, "Tools", "Say Hello"))
           .Then("the command bus executed 'demo.hello'", ctx => ctx.Bus.ExecutedIds.Contains("demo.hello"))
           .AssertPassed();
}
```

* **Given** sets up the DI container, command bus, store, and `MainForm` instance.
* **When** simulates a user action (`PerformClick` on the menu item).
* **Then** asserts that the correct command executed.

---

## Example: State Store Reaction

```csharp
[Scenario("Given MainForm bound to the store; When I set the theme to Dark; Then the form applies the Dark theme")]
[StaFact]
public Task Theme_reacts_to_store_state()
    => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
       .When("the store theme becomes Dark", ctx =>
       {
           ctx.Store.Dispatch(new SetTheme("Dark"));
           return ctx;
       })
       .Then("the form shows Dark theme & status text", ctx =>
           ctx.Form.BackColor == Color.FromArgb(32, 32, 32) &&
           ctx.Form.ForeColor == Color.WhiteSmoke)
       .AssertPassed();
```

---

## E2E Coverage

Your acceptance tests can also cover the **entire application bootstrapping process**, including:

* Building configuration from `appsettings.json`
* Calling `AddAppAllingApplication` and `AddAppAllingUiWinForms`
* Loading plugins via `PluginLoader`
* Constructing `MainForm`
* Ensuring the app runs without exceptions

These are typically run as **integration tests** in CI to produce coverage for the entry point:

```csharp
[Scenario("Given Program.cs wiring; When MainForm is resolved; Then it constructs without error")]
[StaFact]
public async Task Program_to_MainForm_executes_without_exception()
{
    var (services, provider) = await TestBootstrap.CreateProviderAsync();
    var mainForm = provider.GetRequiredService<MainForm>();
    Assert.NotNull(mainForm);
}
```

---

## Best Practices

* **Keep steps reusable:** Extract shared `Given_...`, `When_...`, `Then_...` methods for clarity.
* **Use STA thread tests** (`[StaFact]`) for anything that touches WinForms.
* **Prefer realistic contributions:** Provide real `ICommandContribution` and `IMenuModelContribution` instances, not just mocks.
* **Assert behavior, not implementation details:** Focus on *what the user sees* rather than internal state unless necessary.

---

## See Also

* [Creating a Plugin](../plugins/creating-plugin.md)
* [Menu Composer](../architecture/menu-composer.md)
* [Command Bus](../architecture/command-bus.md)
* [State Store](../architecture/state-store.md)

