# Commands in Plugins

Commands are the primary way plugins interact with the host application.  
They encapsulate actions (like "Open Tool Window" or "Toggle Theme") that can be triggered by menus, toolbars, keyboard shortcuts, or other code.

## Overview

A **command** in AppAlling consists of:

- A **unique identifier** (`Id`): e.g., `"tools.sayHello"`
- A **human-readable title** (`Title`): e.g., `"Say Hello"`
- An **optional keyboard shortcut** (`Shortcut`): e.g., `"Ctrl+H"`

Commands are **registered with the `ICommandBus`** at startup and can be executed by any part of the application.

---

## Declaring Commands

Inside your plugin, implement `ICommandContribution` and return `CommandDescriptor` objects from `DescribeCommands()`:

```csharp
using AppAlling.Abstractions;

public sealed class HelloWorldPlugin : ICommandContribution
{
    public IEnumerable<CommandDescriptor> DescribeCommands() =>
    [
        new("tools.sayHello", "Say Hello"),
        new("view.toggleTheme", "Toggle Theme", "Ctrl+T"),
        new("tool.hello.open", "Open Hello Window")
    ];
}
```

---

## Registering Execution Handlers

Commands are just **declarations** until you register execution handlers.
In `ConfigureServices`, register one or more `ICommandExec` implementations:

```csharp
services.AddSingleton<ICommandExec>(new Exec("tools.sayHello", _ =>
{
    MessageBox.Show("Hello from the plugin!");
    return Task.CompletedTask;
}));
```

`CommandRegistry.ResolveExecuteDelegate()` will pick the correct executor when the command bus is initialized.

---

## Executing Commands

At runtime, commands are executed through the `ICommandBus`:

```csharp
await commandBus.ExecuteAsync("tools.sayHello", serviceProvider);
```

If no handler is found, the bus silently no-ops (no exception is thrown).

---

## Keyboard Shortcuts

If you specify a `Shortcut` in `CommandDescriptor`, the `MainForm` will parse it and wire it up to the menu item automatically:

```csharp
new("view.toggleTheme", "Toggle Theme", "Ctrl+T")
```

---

## Example: Command + Menu + Tool Window

Here’s how commands tie into menus and tool windows together:

```csharp
public sealed class MyPlugin :
    IAppallingPlugin,
    ICommandContribution,
    IMenuModelContribution,
    IToolWindowContribution
{
    public IEnumerable<CommandDescriptor> DescribeCommands() =>
    [
        new("tools.openMyWindow", "Open My Window")
    ];

    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&Tools",
            Children:
            [
                new MenuItemDescriptor("&Open My Window", "tools.openMyWindow")
            ])
    ];

    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("My Window", "tools.openMyWindow")
    ];

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        services.AddSingleton<ICommandExec>(new Exec("tools.openMyWindow", sp =>
        {
            var factory = sp.GetRequiredService<IToolWindowFactory>();
            factory.Create().Show();
            return Task.CompletedTask;
        }));
        services.AddSingleton<IToolWindowFactory>(new MyToolWindowFactory());
    }
}
```

---

## Testing Commands

Use **TinyBDD acceptance tests** to verify your commands are registered and executable:

```csharp
[Scenario("Given a plugin; When I execute tools.sayHello; Then the message box is shown")]
[Fact]
public Task SayHello_command_executes()
    => Given("services with HelloWorldPlugin registered", () =>
       {
           var services = new ServiceCollection();
           var plugin = new HelloWorldPlugin();
           plugin.ConfigureServices(services, new TestPluginContext());
           return services.BuildServiceProvider();
       })
       .When("I execute tools.sayHello", async sp =>
       {
           var exec = sp.GetServices<ICommandExec>()
                        .First(e => e.CanHandle("tools.sayHello"));
           await exec.ExecuteAsync(sp);
           return sp;
       })
       .Then("a message box should be shown", _ => true /* use UI automation or stubbing */)
       .AssertPassed();
```

---

## Best Practices

* **Use consistent naming**
  Prefix command IDs logically (`tools.`, `view.`, `tool.`) to avoid collisions.
* **Keep commands focused**
  Each command should do one thing and do it well.
* **Make commands re-entrant**
  Avoid stateful side effects that prevent repeated invocation.
* **Test your commands**
  Automated tests help catch regressions and ensure menus trigger the right behavior.

---

## See Also

* [Creating a Plugin](creating-plugin.md)
* [Menu Composer Architecture](../architecture/menu-composer.md)
* [Command Bus](../architecture/command-bus.md)
