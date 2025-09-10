using Microsoft.Extensions.DependencyInjection;

namespace AppAlling.UI.WinForms.Commands;

internal static class CommandRegistry
{

    public static Func<IServiceProvider, Task> ResolveExecuteDelegate(IServiceProvider sp, string id)
    {
        var execs = sp.GetServices<ICommandExec>();
        var handler = execs.FirstOrDefault(h => h.CanHandle(id));
        if (handler is not null) return handler.ExecuteAsync;

        return _ => Task.CompletedTask;
    }

    public static Func<Form> ResolveToolWindowFactory(IServiceProvider sp, string id)
    {
        var factories = sp.GetServices<IToolWindowFactory>();
        var f = factories.FirstOrDefault(x => x.CommandId.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (f is not null) return () => f.Create();

        return () => new Form { Text = $"Missing tool window for '{id}'" };
    }
}
