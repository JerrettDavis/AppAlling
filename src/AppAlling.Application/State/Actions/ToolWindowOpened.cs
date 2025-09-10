using AppAlling.Abstractions.State;

namespace AppAlling.Application.State.Actions;

/// <summary>
/// Action indicating a tool window has been opened.
/// </summary>
public sealed record ToolWindowOpened : IAction;