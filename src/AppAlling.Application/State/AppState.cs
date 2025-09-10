namespace AppAlling.Application.State;

/// <summary>
/// Root application state managed by the Store.
/// </summary>
/// <param name="Theme">Current theme name (e.g., "Light" or "Dark").</param>
/// <param name="OpenToolWindowCount">Number of currently open tool windows.</param>
public sealed record AppState(
    string Theme = "Light",
    int OpenToolWindowCount = 0
);