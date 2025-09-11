# Contributing Tool Windows

Tool windows are pluggable forms or panels that a plugin can expose for the user to open via menu items or commands.

---

## TL;DR

- Implement `IToolWindowContribution`.
- Return `ToolWindowDescriptor` with a **title** and **command id**.
- Register an `IToolWindowFactory` that knows how to create the `Form`.
- The host automatically wires a command like `tool.{id}.open` and a menu item if you expose it in `IMenuModelContribution`.

---

## Declaring a Tool Window

```csharp
using AppAlling.Abstractions;

public sealed class HelloWorldPlugin : IToolWindowContribution
{
    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("Hello Window", "tool.hello.open")
    ];
}
```

* **Title**: Display name of the window (used in menus and status updates).
* **CommandId**: Unique identifier that will be registered with the Command Bus.

---

## Creating the Factory

The factory is a small class responsible for creating the `Form` instance:

```csharp
using System.Windows.Forms;
using AppAlling.Abstractions;

public sealed class HelloWindowFactory : IToolWindowFactory
{
    public string CommandId => "tool.hello.open";

    public Form Create()
    {
        return new HelloForm(); // your custom Form
    }
}
```

Register it in `ConfigureServices`:

```csharp
public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
{
    services.AddSingleton<IToolWindowFactory>(new HelloWindowFactory());
}
```

---

## Optional: Menu Integration

Most plugins also contribute a menu item under `&View` or `&Tools`:

```csharp
public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
[
    new("&View",
        Children:
        [
            new MenuItemDescriptor("&Hello Window", "tool.hello.open")
        ])
];
```

This allows the user to open the window from the main menu.

---

## Runtime Behavior

* During host startup, `MainForm.RegisterCommands` automatically:

    * Creates a `CommandDescriptor` for each `ToolWindowDescriptor` you return.
    * Registers a handler that calls `IToolWindowFactory.Create()` and `Show(this)`.
* When the command is executed, your `Form` is created and displayed.

---

## Example: Complete Tool Window Plugin

```csharp
public sealed class HelloWorldPlugin :
    IAppallingPlugin,
    IToolWindowContribution,
    IMenuModelContribution
{
    public PluginMetadata Metadata => new("hello.world", "Hello World", "1.1.0");

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        services.AddSingleton<IToolWindowFactory>(new HelloWindowFactory());
        services.AddSingleton<IToolWindowContribution>(this);
        services.AddSingleton<IMenuModelContribution>(this);
    }

    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("Hello Window", "tool.hello.open")
    ];

    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&View",
            Children:
            [
                new MenuItemDescriptor("&Hello Window", "tool.hello.open")
            ])
    ];
}
```

---

## Best Practices

* **Keep windows lightweight** – defer heavy loading until `Shown` event.
* **Avoid singletons** unless your window must be unique—otherwise let the factory create a fresh instance each time.
* **Use DI** for dependencies in your window (resolve services inside the factory).
* **Use descriptive IDs** – follow the convention `tool.{feature}.open` for clarity and consistency.

---

## Testing Tool Window Wiring

You can write a TinyBDD test to assert that the implicit open command is registered:

```csharp
[Scenario("Given MainForm with a tool window contribution; When constructed; Then an implicit open command is registered")]
[StaFact]
public Task Tool_window_registers_command()
    => Given("MainForm with HelloWorld plugin", Given_mainform_with_demo_contributions)
       .Then("the implicit tool open command is registered", ctx =>
           ctx.Bus.List.ContainsKey("tool.hello.open"))
       .AssertPassed();
```

---

## See Also

* [Creating a Plugin](creating-plugin.md)
* [Contributing Commands](commands.md)
* [Contributing Menus](menus.md)
* [Command Bus](../architecture/command-bus.md)
