# Application Configuration

AppAlling uses a layered configuration model based on `Microsoft.Extensions.Configuration`.  
This allows you to define defaults in `appsettings.json`, override them per environment, and even pass in runtime overrides via environment variables or command-line arguments.

---

## Configuration Sources (Precedence)

When the application starts (`Program.cs`), configuration is loaded in this order (later wins):

1. `appsettings.json` (required)
2. `appsettings.{Environment}.json` (optional, e.g. `appsettings.Development.json`)
3. Environment variables (`ASPNETCORE_` or `DOTNET_` prefixes supported)
4. Command-line arguments
5. User secrets (if running in development)

> **Note:** `EnvironmentName` is read from `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`, defaulting to `Development`.

---

## Example `appsettings.json`

```json
{
  "AppSettings": {
    "Plugins": {
      "Directory": "Plugins"
    },
    "Theme": "Light"
  }
}
````

### Sections

| Section                         | Description                                                 | Default   |
| ------------------------------- | ----------------------------------------------------------- | --------- |
| `AppSettings.Plugins.Directory` | Path to the plugin folder relative to the application root. | `Plugins` |
| `AppSettings.Theme`             | Default UI theme (`Light` or `Dark`).                       | `Light`   |

---

## Overriding Configuration

### 1. Environment Variables

You can override any setting via environment variables by using `__` as a separator:

```powershell
$env:AppSettings__Theme = "Dark"
dotnet run --project src\AppAlling.UI.WinForms
```

This would start the application in **Dark mode**, regardless of what `appsettings.json` says.

### 2. Command-Line Arguments

You can also pass overrides at startup:

```bash
dotnet run --project src/AppAlling.UI.WinForms --AppSettings:Theme=Dark
```

Command-line arguments take precedence over both JSON and environment variables.

---

## Validating Configuration

AppAlling does not fail fast on missing plugin directories — it will simply skip plugin loading if the folder doesn’t exist.
For production deployments, we recommend ensuring that:

* The `Plugins` directory exists and is readable.
* Any critical settings (e.g., remote plugin sources) are validated at startup.

> **Tip:** You can write a smoke test or BDD acceptance scenario that asserts `PluginLoader.LoadPlugins()` returns at least one plugin in a deployment environment.

---

## Changing Environments

You can run the application in different environments:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\AppAlling.UI.WinForms
```

This will load `appsettings.Production.json` if present and apply production-specific overrides.

---

## Recommended Practices

* **Keep `appsettings.json` checked in** with sane defaults for development.
* **Use per-environment files** for staging/production-specific settings.
* **Prefer environment variables or CI/CD secrets** for sensitive values (e.g. API keys).
* **Use `IOptions<AppSettings>`** inside services to access strongly-typed settings.

---

## Next Steps

* [Building & Running](building-running.md) – Run the application with your chosen settings.
* [Plugin Development](../plugins/creating-plugin.md) – Learn how to build and configure your own plugins.

```