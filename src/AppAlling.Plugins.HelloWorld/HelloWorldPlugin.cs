using System.Windows.Forms;
using AppAlling.Abstractions;
using AppAlling.Abstractions.State;
using AppAlling.Application.State;
using AppAlling.Application.State.Actions;
using AppAlling.UI.WinForms.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.Plugins.HelloWorld;

/// <summary>
/// Sample plugin demonstrating command, menu, and tool window contributions for AppAlling.
/// </summary>
public sealed class HelloWorldPlugin :
    IAppallingPlugin,
    ICommandContribution,
    IMenuModelContribution,
    IToolWindowContribution
{
    /// <summary>
    /// Basic plugin identification used by the host.
    /// </summary>
    public PluginMetadata Metadata => new("hello.world", "Hello World", "1.1.0");

    /// <summary>
    /// Registers this plugin's contributions and execution handlers into DI.
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IPluginContext ctx)
    {
        // Register contribution surfaces via DI
        services.AddSingleton<ICommandContribution>(this);
        services.AddSingleton<IMenuModelContribution>(this);
        services.AddSingleton<IToolWindowContribution>(this);

        // Register executors / factories
        services.AddSingleton<ICommandExec>(new Exec("tools.sayHello", SayHello));
        services.AddSingleton<ICommandExec>(new Exec("view.toggleTheme", SetTheme));

        services.AddSingleton<IToolWindowFactory>(new HelloWindowFactory());
    }

    private static Task SayHello(IServiceProvider sp)
    {
        MessageBox.Show("Hello from HelloWorld plugin!", "AppAlling");
        return Task.CompletedTask;
    }

    private static Task SetTheme(IServiceProvider sp)
    {
        var store = sp.GetRequiredService<IStore<AppState>>();
        var next = store.Current.Theme == "Light" ? "Dark" : "Light";
        store.Dispatch(new SetTheme(next));
        return Task.CompletedTask;
    }
    

    /// <summary>
    /// Declares the commands provided by this plugin.
    /// </summary>
    public IEnumerable<CommandDescriptor> DescribeCommands() =>
    [
        new("tools.sayHello", "Say Hello"),
        new("view.toggleTheme", "Toggle Theme", "Ctrl+T"),
        new("tool.hello.open", "Open Hello Window")
    ];

    /// <summary>
    /// Builds the menu model contributed by this plugin.
    /// </summary>
    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&Tools",
            Children:
            [
                new MenuItemDescriptor("Say &Hello", "tools.sayHello")
            ]),
        new("&View",
            Children:
            [
                new MenuItemDescriptor("&Toggle Theme", "view.toggleTheme"),
                new MenuItemDescriptor("&Hello Window", "tool.hello.open")
            ])
    ];

    /// <summary>
    /// Describes tool windows contributed by this plugin.
    /// </summary>
    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("Hello Window", "tool.hello.open")
    ];

    // --- helpers ---
    private sealed class Exec(
        string id,
        Func<IServiceProvider, Task> run
    ) : ICommandExec
    {
        public bool CanHandle(string commandId) =>
            string.Equals(
                commandId,
                id,
                StringComparison.OrdinalIgnoreCase);

        public Task ExecuteAsync(IServiceProvider services) => run(services);
    }

    private sealed class HelloWindowFactory : IToolWindowFactory
    {
        public string CommandId => "tool.hello.open";
        public Form Create() => new HelloForm();
    }
}