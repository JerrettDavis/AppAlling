namespace AppAlling.Abstractions;

/// <summary>
/// Declarative description of a tool window contributed by a plugin.
/// </summary>
/// <param name="Title">Caption text to display on the tool window.</param>
/// <param name="CommandId">Command id that opens this window; the host wires an implicit command to show the window.</param>
public sealed record ToolWindowDescriptor(
    string Title,
    string CommandId
);