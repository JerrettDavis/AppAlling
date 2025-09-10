using AppAlling.Abstractions;

namespace AppAlling.Application.Commands;

/// <summary>
/// Simple in-memory command bus that maps command ids to execution delegates.
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Registers or replaces a command entry.
    /// </summary>
    /// <param name="descriptor">Command metadata (id, title, etc.).</param>
    /// <param name="executeAsync">Execution delegate resolved at runtime via DI.</param>
    void Register(CommandDescriptor descriptor, Func<IServiceProvider, Task> executeAsync);

    /// <summary>
    /// Executes a command by id if registered; otherwise no-ops.
    /// </summary>
    /// <param name="id">Command identifier.</param>
    /// <param name="services">Service provider passed to the handler.</param>
    Task ExecuteAsync(string id, IServiceProvider services);

    /// <summary>
    /// Current snapshot of registered command descriptors, keyed by id (case-insensitive).
    /// </summary>
    IReadOnlyDictionary<string, CommandDescriptor> List { get; }
}