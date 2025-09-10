using AppAlling.Abstractions.State;

namespace AppAlling.Application.State.Actions;

/// <summary>
/// Action to set the current theme name (e.g., "Light" or "Dark").
/// </summary>
/// <param name="Theme">The desired theme name.</param>
public sealed record SetTheme(string Theme) : IAction;