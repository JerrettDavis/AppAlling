# Creating a Plugin

Plugins extend AppAlling by contributing commands, menus, and tool windows without modifying the host application.  
This guide walks through the **complete process of authoring a plugin**, from project setup to registration and testing.

---

## 1. Create a Class Library

Create a new `.NET` class library targeting `net8.0-windows` or higher:

```bash
dotnet new classlib -n MyCompany.MyPlugin -f net8.0-windows
````

Add a reference to the AppAlling abstractions:

```bash
dotnet add package AppAlling.Abstractions
dotnet add package AppAlling.UI.WinForms.Abstractions
```

---

## 2. Implement `IAppallingPlugin`

Every plugin must provide a root implementation that:

* Exposes `PluginMetadata`
* Registers its contributions and executors via DI

```csharp
using AppAlling.Abstractions;
using AppAlling.Abstractions.State;
using Microsoft.Extensions.DependencyInjection;

namespace MyCompany.MyPlugin;

public sealed class MyPlugin :
    IAppallingPlugin,
    ICommandContribution,
    IMenuModelContribution,
    IToolWindowContribution
{
    public PluginMetadata Metadata => new("my.plugin", "My Plugin", "1.0.0");

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        services.AddSingleton<ICommandContribution>(this);
        services.AddSingleton<IMenuModelContribution>(this);
        services.AddSingleton<IToolWindowContribution>(this);

        // Register executors
        services.AddSingleton<ICommandExec>(new Exec("tools.showMessage", _ =>
        {
            MessageBox.Show($"Plugin running from: {ctx.RootDirectory}");
            return Task.CompletedTask;
        }));

        services.AddSingleton<IToolWindowFactory>(new MyToolWindowFactory());
    }

    public IEnumerable<CommandDescriptor> DescribeCommands() =>
    [
        new("tools.showMessage", "Show Message"),
        new("tool.myWindow.open", "Open My Tool Window")
    ];

    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&Tools",
            Children:
            [
                new MenuItemDescriptor("Show &Message", "tools.showMessage")
            ])
    ];

    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("My Tool Window", "tool.myWindow.open")
    ];

    // Executor wrapper
    private sealed class Exec(string id, Func<IServiceProvider, Task> run) : ICommandExec
    {
        public bool CanHandle(string commandId) => string.Equals(commandId, id, StringComparison.OrdinalIgnoreCase);
        public Task ExecuteAsync(IServiceProvider services) => run(services);
    }

    private sealed class MyToolWindowFactory : IToolWindowFactory
    {
        public string CommandId => "tool.myWindow.open";
        public Form Create() => new MyToolWindow();
    }
}
```

---

## 3. Create a Tool Window

A tool window is a standard `Form` that can be shown from the command bus:

```csharp
using System.Windows.Forms;

namespace MyCompany.MyPlugin;

public sealed class MyToolWindow : Form
{
    public MyToolWindow()
    {
        Text = "My Tool Window";
        Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Hello from My Plugin!",
            Left = 10,
            Top = 10
        });
    }
}
```

---

## 4. Build and Deploy

Build the project:

```bash
dotnet build -c Release
```

Copy the resulting `.dll` into the host application's `Plugins` folder.
When the app starts, the `PluginLoader` will discover and configure it automatically.

---

## 5. Verify with Acceptance Tests

The recommended approach is **behavior-driven testing** using [TinyBDD](https://github.com/JerrettDavis/TinyBDD):

```csharp
[Feature("MyPlugin – basic functionality")]
public class MyPluginAcceptanceTests : TinyBddXunitBase
{
    [Scenario("Given the plugin is loaded; When I execute tools.showMessage; Then a message box is shown")]
    [Fact]
    public Task Executes_showMessage_command()
        => Given("a ServiceCollection with MyPlugin registered", () =>
           {
               var services = new ServiceCollection();
               var plugin = new MyPlugin();
               plugin.ConfigureServices(services, new TestPluginContext());
               return services.BuildServiceProvider();
           })
           .When("I execute tools.showMessage", async sp =>
           {
               var exec = sp.GetRequiredService<ICommandExec>();
               await exec.ExecuteAsync(sp);
               return sp;
           })
           .Then("the message box should be shown", _ => true /* use UI automation or test harness */)
           .AssertPassed();
}
```

---

## Best Practices

* **Use semantic IDs**: Prefix command IDs (`tools.`, `view.`, `tool.`) for consistency.
* **Leverage DI**: Prefer constructor injection for services (loggers, stores, APIs).
* **Keep contributions atomic**: Commands should do one thing well.
* **Provide shortcuts**: Define `ShortcutKeys` in `CommandDescriptor` when appropriate.
* **Include metadata**: Unique plugin IDs prevent collisions and aid diagnostics.

---

## See Also

* [Plugin Loader Architecture](../architecture/plugin-loader.md)
* [Command Bus](../architecture/command-bus.md)
* [Menu Composer](../architecture/menu-composer.md)

