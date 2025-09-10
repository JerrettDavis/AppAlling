using AppAlling.Abstractions.State;

namespace AppAlling.Application.State.Actions;

/// <summary>
/// Action indicating a tool window has been closed.
/// </summary>
public sealed record ToolWindowClosed : IAction;