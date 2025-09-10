namespace AppAlling.UI.WinForms.Commands;

/// <summary>
/// Resolves and executes UI commands by id. Implementations are discovered via DI.
/// </summary>
public interface ICommandExec
{
    /// <summary>
    /// Returns true if this handler can execute the specified command id.
    /// </summary>
    /// <param name="commandId">The command id to check.</param>
    /// <returns>True if the command id can be handled; otherwise, false.</returns>
    bool CanHandle(string commandId);

    /// <summary>
    /// Executes the command using the provided service provider.
    /// </summary>
    /// <param name="services">The service provider to use for command execution.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(IServiceProvider services);
}