# AppAlling 🧩

AppAlling is a **pluggable, reactive WinForms shell** written in .NET 9.  
It provides a minimal, opinionated host that loads external plugins to compose:

- **Menus** and commands
- **Tool windows** and views
- **Reactive global state** shared across plugins
- **Services** and background tasks

The shell itself contains almost no business logic.  
Everything is contributed by plugins through well-defined contracts.

---

## ✨ Features

- **Plugin-first design** — Drop `*.dll` plugins into `/plugins` and they light up the shell
- **Centralized state management** — Redux-style `Store<T>` with Rx observables
- **Dependency injection** — `Microsoft.Extensions.DependencyInjection` powers DI
- **Data-driven UI** — Menus and commands are described declaratively (no hard-coded WinForms controls)

---

## 📦 Solution Layout

```

AppAlling/
├─ src/
│  ├─ AppAlling.Abstractions/       # Contracts for plugins (no UI deps)
│  ├─ AppAlling.Application/        # State store, command bus, DI extensions
│  ├─ AppAlling.PluginHost/         # Assembly discovery and composition
│  ├─ AppAlling.UI.WinForms/        # Shell (minimal WinForms host)
│  └─ AppAlling.Plugins.HelloWorld/ # Example plugin (menus + tool window)
└─ plugins/                         # Runtime plugin folder (copied here at build)

````

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows (WinForms)

### Build & Run

```bash
dotnet build
dotnet run --project src/AppAlling.UI.WinForms
````

On build, sample plugins are copied to `/plugins`, so they’ll load automatically.

---

## 🧩 Writing a Plugin

Create a new class library referencing `AppAlling.Abstractions`:

```bash
dotnet new classlib -n MyCoolPlugin
dotnet add MyCoolPlugin reference src/AppAlling.Abstractions/AppAlling.Abstractions.csproj
```

Implement `IAppallingPlugin` and optional contribution interfaces:

```csharp
public sealed class MyPlugin : IAppallingPlugin, IMenuModelContribution, ICommandContribution
{
    public PluginMetadata Metadata => new("my.plugin", "My Plugin", "1.0.0");

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        // Register any services, command handlers, or tool window factories
        services.AddSingleton<ICommandExec>(new Exec("myPlugin.sayHi", _ =>
        {
            MessageBox.Show("Hello from MyPlugin!");
            return Task.CompletedTask;
        }));
    }

    public IEnumerable<CommandDescriptor> DescribeCommands() => new[]
    {
        new CommandDescriptor("myPlugin.sayHi", "Say Hi")
    };

    public IEnumerable<MenuItemDescriptor> BuildMenuModel() => new[]
    {
        new MenuItemDescriptor("&Tools", Children: new []
        {
            new MenuItemDescriptor("Say &Hi", "myPlugin.sayHi")
        })
    };

    private sealed class Exec(string id, Func<IServiceProvider, Task> run) : ICommandExec
    {
        public bool CanHandle(string commandId) => commandId == id;
        public Task ExecuteAsync(IServiceProvider sp) => run(sp);
    }
}
```

Build your plugin and copy the DLL to `plugins/`. Launch AppAlling — your menu item will appear instantly.

---

## 🏗 Extensibility Points

| Contract                  | Purpose                               |
| ------------------------- | ------------------------------------- |
| `ICommandContribution`    | Declare commands (metadata)           |
| `IMenuModelContribution`  | Contribute to menu tree (data-driven) |
| `IToolWindowContribution` | Register tool windows by command ID   |
| `ICommandExec`            | Provide execution logic for commands  |
| `IToolWindowFactory`      | Build `Form`/`UserControl` instances  |
| `IStore<AppState>`        | Access centralized, reactive state    |

---


## 📜 License

MIT — do whatever you want, but a link back or credit is appreciated.

