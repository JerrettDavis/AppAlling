# Plugin Metadata & Context

Each plugin must provide **basic identification metadata** and is optionally given a **context** that describes its runtime environment (e.g., root directory).  

This metadata is used by the host to display information in logs, debug UIs, and to manage version compatibility.

---

## Declaring Metadata

Every plugin must implement `IAppallingPlugin` and return a `PluginMetadata` object:

```csharp
using AppAlling.Abstractions;

public sealed class HelloWorldPlugin : IAppallingPlugin
{
    public PluginMetadata Metadata => new(
        Id: "hello.world",
        Name: "Hello World",
        Version: "1.1.0"
    );

    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        // register contributions and services here
    }
}
```

### Metadata Fields

| Property    | Type     | Purpose                                                                                 |
| ----------- | -------- | --------------------------------------------------------------------------------------- |
| **Id**      | `string` | Unique plugin identifier (use a reverse-DNS or namespaced format, e.g., `hello.world`). |
| **Name**    | `string` | Human-readable name displayed in logs or plugin info screens.                           |
| **Version** | `string` | Semantic version (e.g. `1.0.0`) or any identifier meaningful to the host.               |

---

## Understanding `IPluginContext`

When the host loads plugins, it passes an `IPluginContext` to `ConfigureServices`:

```csharp
public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
{
    var root = ctx.RootDirectory;
    // Use root to locate plugin resources, configs, etc.
}
```

### `RootDirectory`

* Always points to the folder that contains the plugin assembly.
* Useful for locating:

    * Additional resource files (images, configs)
    * Data that ships with the plugin
    * Relative paths for logging/debugging

---

## Example: Using Context for Resources

```csharp
public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
{
    var resourcesPath = Path.Combine(ctx.RootDirectory, "Resources");
    if (Directory.Exists(resourcesPath))
    {
        // register or preload resources as needed
    }
}
```

---

## Host Behavior

* **PluginLoader** scans a directory for `.dll` files, creates plugin instances, and builds a `Dictionary<IAppallingPlugin, IPluginContext>`.
* The `RootDirectory` is inferred from the physical location of the assembly file.
* During `ConfigureAll`, the host passes each plugin’s context to its `ConfigureServices` method.

---

## Best Practices

* Keep `Id` globally unique to avoid collisions.
* Use the `RootDirectory` instead of hardcoding paths – this allows the plugin to be portable.
* Consider versioning for compatibility checks in the future (e.g., enforce minimum host version).

---

## See Also

* [Creating a Plugin](creating-plugin.md)
* [Contributing Commands](commands.md)
* [Plugin Loader](../architecture/plugin-loader.md)
