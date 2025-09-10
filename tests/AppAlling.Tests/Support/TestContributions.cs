using System.Windows.Forms;
using AppAlling.Abstractions;
using AppAlling.UI.WinForms.Commands;

namespace AppAlling.Tests.Support;

internal sealed class DemoCommandContribution : ICommandContribution
{
    public IEnumerable<CommandDescriptor> DescribeCommands() =>
    [
        new("demo.hello", "Hello", "Ctrl+H")
    ];
}

internal sealed class DemoMenuContribution : IMenuModelContribution
{
    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&Tools", Children:
        [
            new MenuItemDescriptor("&Say Hello", "demo.hello")
        ])
    ];
}

internal sealed class DemoToolWindowContribution : IToolWindowContribution
{
    public IEnumerable<ToolWindowDescriptor> DescribeToolWindows() =>
    [
        new("Inspector", "tool.inspector.open")
    ];
}

internal sealed class InspectorFactory : IToolWindowFactory
{
    public string CommandId => "tool.inspector.open";
    public Form Create() => new() { Text = "Inspector" };
}

internal sealed class HelloExec : ICommandExec
{
    public bool CanHandle(string commandId) => string.Equals(commandId, "demo.hello", StringComparison.OrdinalIgnoreCase);

    public Task ExecuteAsync(IServiceProvider services)
    {
        // side-effect not required; bus.ExecuteAsync recording is enough
        return Task.CompletedTask;
    }
}