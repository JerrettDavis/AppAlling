using AppAlling.Abstractions.State;
using AppAlling.Application.State.Actions;

namespace AppAlling.Application.State;

/// <summary>
/// Pure reducer functions that transform application state in response to actions.
/// </summary>
public static class Reducers
{
    /// <summary>
    /// Root reducer that handles all known actions and returns the next <see cref="AppState"/>.
    /// </summary>
    public static AppState Root(AppState s, IAction a) => a switch
    {
        SetTheme t       => s with { Theme = t.Theme },
        ToolWindowOpened => s with { OpenToolWindowCount = s.OpenToolWindowCount + 1 },
        ToolWindowClosed => s with { OpenToolWindowCount = s.OpenToolWindowCount - 1 },
        _                => s
    };
}
