
# AppAlling

Welcome to **AppAlling** — a pluggable, extensible WinForms desktop shell built on modern .NET 9 practices.

AppAlling is intentionally "boring by design."  
The host application contains very little logic of its own; its main job is to:

- Wire up **dependency injection** (via `Microsoft.Extensions.DependencyInjection`)
- Provide a **centralized state store** (Redux-like, using `IStore<T>`)
- Load plugins from a designated **plugins folder**
- Compose menus, commands, and tool windows dynamically at runtime
- Host the main application shell (`MainForm`)

Everything else — commands, menus, view models, tool windows, and services — comes from **plugins**.

---

## ✨ Features

- **Reactive / Observable state**: based on `System.Reactive` for one-way data flow and change subscriptions.
- **Command bus** with named `CommandDescriptor` objects and async execution support.
- **Menu composer** that merges plugin-provided menu models into the main menu strip.
- **Tool window factories** that allow plugins to register their own forms and open them on demand.
- **DI-first architecture** using `IServiceCollection` and `IServiceProvider`.
- **Pluggable**: Drop new assemblies into the `Plugins` folder to extend functionality without touching host code.

---

## 🚀 Getting Started

### 1. Clone and Build

```bash
git clone https://github.com/yourorg/AppAlling.git
cd AppAlling
dotnet build
````

### 2. Run the Host

```bash
dotnet run --project src/AppAlling.UI.WinForms
```

The app will start, load any plugins found in the `Plugins` directory, and build the menu dynamically.

---

## 🧩 Creating a Plugin

A plugin implements one or more of the following contribution interfaces:

* `ICommandContribution` – declares commands (IDs, titles, shortcuts)
* `IMenuModelContribution` – provides a menu hierarchy (`MenuItemDescriptor`)
* `IToolWindowContribution` – declares tool windows (title + command ID)
* `IAppallingPlugin` – exposes `PluginMetadata` and registers services via `ConfigureServices`

### Minimal Example

```csharp
public sealed class HelloWorldPlugin :
    IAppallingPlugin,
    ICommandContribution,
    IMenuModelContribution
{
    public PluginMetadata Metadata => new("hello.world", "Hello World", "1.0.0");

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        services.AddSingleton<ICommandContribution>(this);
        services.AddSingleton<IMenuModelContribution>(this);
        services.AddSingleton<ICommandExec>(new Exec("tools.sayHello", _ =>
        {
            MessageBox.Show("Hello from plugin!");
            return Task.CompletedTask;
        }));
    }

    public IEnumerable<CommandDescriptor> DescribeCommands()
        => [new("tools.sayHello", "Say Hello")];

    public IEnumerable<MenuItemDescriptor> BuildMenuModel()
        => [new("&Tools", Children: [new MenuItemDescriptor("Say &Hello", "tools.sayHello")])];
}
```

Drop the resulting DLL in `Plugins` and run the host — your command and menu item appear instantly.

---

## 🧪 Testing

AppAlling uses **[TinyBDD](https://github.com/JerrettDavis/TinyBDD)** for behavior-driven tests.
Example scenario from the test suite:

```csharp
[Scenario("Given MainForm with demo contributions; When I click Tools → Say Hello; Then the bus executes 'demo.hello'")]
[StaFact]
public Task Clicking_menu_executes_command()
    => Given("MainForm with demo contributions", Given_mainform_with_demo_contributions)
       .When("I click Tools → Say Hello", ctx => When_I_click_menu_item(ctx.Form, "Tools", "Say Hello"))
       .Then("the command bus executed 'demo.hello'", ctx => Then_bus_executed(ctx.Bus, "demo.hello"))
       .AssertPassed();
```

---

## 📂 Project Structure

```
src/
  AppAlling.Abstractions/     # Core contracts (ICommandContribution, IToolWindowContribution, etc.)
  AppAlling.Application/      # State store, reducers, command bus
  AppAlling.UI.WinForms/      # MainForm, MenuComposer, DI setup
  AppAlling.PluginHost/       # PluginLoader, context creation
  AppAlling.Plugins.HelloWorld/ # Sample plugin

tests/
  AppAlling.Tests/            # Behavior-driven acceptance tests
  AppAlling.Plugins.HelloWorld.Tests/ # Sample plugin tests
```

---

## 🤝 Contributing

1. Fork the repo & create a feature branch
2. Add your plugin / fix / feature
3. Add **TinyBDD** scenarios for new behavior
4. Submit a PR — we love good tests ❤️



