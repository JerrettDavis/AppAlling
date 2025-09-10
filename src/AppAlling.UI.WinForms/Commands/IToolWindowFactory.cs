namespace AppAlling.UI.WinForms.Commands;

/// <summary>
/// Factory that creates tool window instances for a specific command id.
/// </summary>
public interface IToolWindowFactory
{
    /// <summary>
    /// Command id that triggers opening this tool window.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Creates a new tool window Form instance.
    /// </summary>
    Form Create();
}