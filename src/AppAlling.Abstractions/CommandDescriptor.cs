namespace AppAlling.Abstractions;
/// <summary>
/// Describes a command exposed by a plugin.
/// </summary>
/// <param name="Id">Globally unique command identifier (e.g., "view.toggleTheme").</param>
/// <param name="Title">Human-friendly label shown in menus, toolbars, and palette.</param>
/// <param name="Shortcut">Optional keyboard shortcut in a platform-agnostic format, e.g. "Ctrl+Shift+T".</param>
/// <param name="Icon">Logical icon id or path for UI surfaces (optional).</param>
public sealed record CommandDescriptor(
    string Id,
    string Title,
    string? Shortcut = null,
    string? Icon = null
);
