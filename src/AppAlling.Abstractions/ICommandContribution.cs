namespace AppAlling.Abstractions;
/// <summary>
/// Allows a plugin to declare the set of commands it provides.
/// The host collects these descriptors and wires execution delegates at runtime.
/// </summary>
public interface ICommandContribution
{
    /// <summary>
    /// Returns the commands (metadata only) exposed by this plugin.
    /// Execution delegates are provided by the host using dependency injection.
    /// </summary>
    IEnumerable<CommandDescriptor> DescribeCommands();
    // The shell wires the execution delegate at runtime:
    // Func<IServiceProvider, Task> ExecuteAsync is provided via registration (see below).
}
