# End-to-End (E2E) Coverage

End-to-end (E2E) tests exercise the **entire application stack** — from `Program.cs` through dependency injection, plugin loading, command registration, menu composition, and UI event wiring — to ensure the app behaves correctly when run as a whole.

E2E tests also contribute to **code coverage**, verifying that critical paths are executed in realistic scenarios.

---

## Why E2E Coverage Matters

- **Realistic verification:** Confirms that DI, configuration, and plugin discovery work together as expected.
- **Regression safety:** Detects issues that unit tests may miss, such as misconfigured `ServiceCollection` or missing plugin registrations.
- **Coverage boost:** Ensures top-level wiring (e.g., `Program.cs`, `PluginLoader.ConfigureAll`, `MainForm`) is counted in coverage metrics.

---

## Setting Up E2E Tests

Use `xUnit` + `TinyBDD.Xunit.v3` with `[StaFact]` to ensure tests run on the **UI thread**.

Example: *Program → MainForm → Click Menu Item → Assert Command Execution*

```csharp
using AppAlling.Application;
using AppAlling.PluginHost;
using AppAlling.UI.WinForms;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;

[Feature("Application E2E – Program through MainForm")]
public class ProgramE2ETests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Given Program wiring; When MainForm is created; Then menus and commands are present")]
    [StaFact]
    public Task Program_to_MainForm_smoke_test()
        => Given("Program-like DI setup", () =>
        {
            var services = new ServiceCollection()
                .AddAppAllingApplication()
                .AddAppAllingUiWinForms(AppAlling.UI.WinForms.DependencyInjection.BuildConfiguration())
                .AddTransient<MainForm>();

            var loader = new PluginLoader("Plugins");
            var plugins = loader.LoadPlugins(out var ctxs);
            PluginLoader.ConfigureAll(services, plugins, ctxs);

            return services.BuildServiceProvider();
        })
        .When("I resolve MainForm", sp => sp.GetRequiredService<MainForm>())
        .Then("MainForm is not null", form => form is not null)
        .And("menus were populated", form => form.MainMenuStrip!.Items.Count > 0)
        .AssertPassed();
}
```

---

## Running E2E Tests with Coverage

1. **Run tests with coverlet or built-in .NET coverage:**

```bash
dotnet test --collect:"XPlat Code Coverage"
```

2. **Use ReportGenerator (optional) to create a readable HTML report:**

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
```

Then open `coveragereport/index.html` in a browser.

---

## Best Practices

* **Keep E2E tests minimal:** Focus on smoke tests and critical workflows (menu clicks, command execution, state changes).
* **Avoid UI flakiness:** Do not show dialogs or block with modal windows; use test-friendly factories or mocks.
* **Run under CI:** Include E2E coverage as part of your pipeline to ensure full coverage metrics.

---

## See Also

* [Unit Tests](unit-tests.md)
* [Acceptance Tests](acceptance-tests.md)
* [Architecture Overview](../architecture/overview.md)

