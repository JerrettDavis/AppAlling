namespace AppAlling.Abstractions;

/// <summary>
/// Descriptor for a menu item in the host menu model.
/// </summary>
/// <param name="Title">Display text for the menu item (supports '&amp;' mnemonic).</param>
/// <param name="CommandId">Optional command id to execute when clicked; null indicates a submenu header.</param>
/// <param name="Children">Optional child menu items (submenu entries).</param>
/// <param name="Icon">Optional icon id or path for UI rendering.</param>
public sealed record MenuItemDescriptor(
    string Title,
    string? CommandId = null,
    IEnumerable<MenuItemDescriptor>? Children = null,
    string? Icon = null
);
