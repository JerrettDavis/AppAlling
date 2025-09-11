# Plugin Loader

The **Plugin Loader** is responsible for discovering, loading, and initializing all AppAlling plugins at runtime.  
It enables the host application to remain lightweight while allowing external assemblies to contribute commands, menus, tool windows, and services dynamically.

---

## Goals

- **Discovery** – Scan a directory for `.dll` assemblies containing `IAppallingPlugin` implementations.
- **Isolation** – Provide each plugin with its own `IPluginContext`, including its root directory.
- **Composition** – Invoke `ConfigureServices` on each plugin so it can register its contributions in the host's DI container.

---

## API

```csharp
public sealed class PluginLoader
{
    public PluginLoader(string pluginDirectory);

    public IReadOnlyList<IAppallingPlugin> LoadPlugins(
        out Dictionary<IAppallingPlugin, IPluginContext> contexts);

    public static void ConfigureAll(
        IServiceCollection services,
        IEnumerable<IAppallingPlugin> plugins,
        Dictionary<IAppallingPlugin, IPluginContext> ctxs);
}
```

* **pluginDirectory** – Path to the folder containing plugin assemblies.
* **LoadPlugins** – Loads all valid plugins, returning them and populating the `contexts` dictionary.
* **ConfigureAll** – Invokes each plugin's `ConfigureServices` method to register its contributions.

---

## Plugin Contexts

Each successfully loaded plugin receives a **context**:

```csharp
private sealed class DefaultPluginContext(string root) : IPluginContext
{
    public string RootDirectory => root;
}
```

This allows plugins to locate configuration files, resources, or other assets relative to their DLL.

---

## Discovery Process

1. **Directory Scan** – Finds all `*.dll` files under `pluginDirectory`.
2. **Assembly Load** – Calls `Assembly.LoadFrom(dll)` for each file, skipping those that fail to load.
3. **Type Filtering** – Selects all non-abstract types implementing `IAppallingPlugin`.
4. **Instance Creation** – Uses `Activator.CreateInstance` to instantiate plugins.
5. **Context Association** – Creates a `DefaultPluginContext` for each loaded plugin.

Assemblies that fail to load or contain no valid plugin types are **silently ignored**.

---

## Example Usage

```csharp
var loader = new PluginLoader("Plugins");
var plugins = loader.LoadPlugins(out var contexts);

PluginLoader.ConfigureAll(services, plugins, contexts);
```

At this point, `services` contains all `ICommandContribution`, `IMenuModelContribution`, and `IToolWindowContribution` registrations from each plugin.

---

## Behavioral Guarantees

* **Safe Fallback** – If the directory does not exist, `LoadPlugins` returns an empty list.
* **Graceful Failure** – Malformed DLLs or instantiation failures are ignored, preventing crashes.
* **Context Fidelity** – `RootDirectory` always points to the plugin’s physical location, even if multiple copies of the same DLL exist.

---

## Testing

Behavior-driven acceptance tests cover key scenarios:

* **Empty folder** – Produces no plugins.
* **Valid plugin folder** – Returns plugin and context.
* **Mixed valid/invalid DLLs** – Skips bogus files, loads valid ones.
* **ConfigureAll** – Verifies that contributions are registered and resolvable via DI.

```csharp
[Scenario("Given a folder containing the HelloWorld plugin; When I load plugins; Then exactly one plugin and a valid context are returned")]
public Task Loads_helloworld_plugin_and_context() { ... }
```

---

## Best Practices for Plugin Authors

* Always provide a unique `PluginMetadata` identifier and semantic version.
* Use `ICommandContribution`, `IMenuModelContribution`, and `IToolWindowContribution` to surface functionality.
* Prefer **constructor injection** for dependencies — the host will resolve them through DI.

---

## Related Topics

* [Architecture Overview](overview.md)
* [Command Bus](command-bus.md)
* [Menu Composer](menu-composer.md)
