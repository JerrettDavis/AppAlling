using System.Collections.Concurrent;
using AppAlling.Abstractions;

namespace AppAlling.Application.Commands;

/// <summary>
/// Default in-memory implementation of <see cref="ICommandBus"/>.
/// Thread-safe registration and lookup by id (case-insensitive).
/// </summary>
public sealed class CommandBus : ICommandBus
{
    private readonly ConcurrentDictionary<string, (CommandDescriptor Desc, Func<IServiceProvider, Task> Exec)> _map
        = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, CommandDescriptor> List
        => _map
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Desc,
                StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(
        CommandDescriptor descriptor,
        Func<IServiceProvider, Task> executeAsync)
        => _map[descriptor.Id] = (descriptor, executeAsync);

    /// <inheritdoc />
    public Task ExecuteAsync(string id, IServiceProvider services)
        => _map.TryGetValue(id, out var entry)
            ? entry.Exec(services)
            : Task.CompletedTask;
}