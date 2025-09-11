# Building & Running AppAlling

This guide walks you through building, running, and testing **AppAlling** locally.  
The goal is to get you from cloning the repo to a running instance of the main UI window with plugins loaded.

---

## Prerequisites

Before you start, make sure you have:

- **.NET 9.0 SDK** or later installed  
  Verify with:
  ```bash
  dotnet --version
    ```

* **Windows 10/11** (required for WinForms host)
* (Optional) **JetBrains Rider** or **Visual Studio 2022** for the best developer experience
* (Optional) **Git** for source control and plugin development

---

## Cloning the Repository

Clone the repo to your local machine:

```bash
git clone https://github.com/YourOrg/AppAlling.git
cd AppAlling
```

---

## Building

Use the standard .NET CLI:

```bash
dotnet build AppAlling.sln
```

This will restore dependencies, compile all projects, and produce binaries under `src/**/bin/Debug/net9.0-windows/`.

> **Tip:** The solution uses [Directory.Build.props/targets](../architecture/overview.md) to share common settings and target frameworks across all projects.

---

## Running the Application

The entry point is `Program.cs` in the `AppAlling.UI.WinForms` project.

Run with:

```bash
dotnet run --project src/AppAlling.UI.WinForms
```

This will:

1. Load configuration from:

    * `appsettings.json`
    * `appsettings.{Environment}.json`
    * environment variables
2. Build a DI container with:

    * `AppAlling.Application` (state store + command bus)
    * `AppAlling.UI.WinForms` (UI services + configuration binding)
    * Loaded plugins from the `Plugins` folder
3. Start the **MainForm** and display the menu & status bar

---

## Plugin Loading

By default, plugins are loaded from the `Plugins` folder in the solution root.

To add a plugin:

1. Build the plugin project (or drop a compatible `.dll` into the folder).
2. Relaunch the application.
3. The plugin’s menus, commands, and tool windows should appear automatically.

---

## Running Tests

To execute all automated tests (unit, integration, and acceptance):

```bash
dotnet test AppAlling.sln
```

This will run:

* **TinyBDD acceptance tests** (behavior-driven specs)
* **Unit tests** for command bus, store, and UI composition
* **End-to-end coverage tests** that wire up DI and simulate the main loop

> **Tip:** Generate coverage reports with:
>
> ```bash
> dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
> ```

---

## Troubleshooting

* **Missing appsettings.json** → Ensure `appsettings.json` exists at the solution root.
* **No plugins load** → Check that the `Plugins` folder contains valid plugin assemblies and that `PluginLoader` has read access.
* **Cross-thread UI errors** → Verify that the application runs with `STAThread` set (required for WinForms).

---

## Next Steps

* [Explore the Architecture](../architecture/overview.md) to understand state management, the command bus, and plugin wiring.
* [Write Your First Plugin](../plugins/creating-plugin.md) to extend the application with custom menus and tool windows.
