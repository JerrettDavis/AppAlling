// tests/Support/RecordingCommandBus.cs

using AppAlling.Abstractions;
using AppAlling.Application.Commands;

namespace AppAlling.Tests.Support;

internal sealed class RecordingCommandBus : ICommandBus
{
    private readonly Dictionary<string, (CommandDescriptor Desc, Func<IServiceProvider, Task> Exec)> _map
        = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ExecutedIds { get; } = [];

    public IReadOnlyDictionary<string, CommandDescriptor> List
        => _map.ToDictionary(kv => kv.Key, kv => kv.Value.Desc, StringComparer.OrdinalIgnoreCase);

    public void Register(CommandDescriptor descriptor, Func<IServiceProvider, Task> executeAsync)
        => _map[descriptor.Id] = (descriptor, executeAsync);

    public async Task ExecuteAsync(string id, IServiceProvider services)
    {
        ExecutedIds.Add(id);
        if (_map.TryGetValue(id, out var e))
            await e.Exec(services);
    }
}