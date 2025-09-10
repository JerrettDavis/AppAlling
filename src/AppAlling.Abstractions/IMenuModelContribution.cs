namespace AppAlling.Abstractions;
/// <summary>
/// Allows a plugin to contribute menu structure to the host shell.
/// Return top-level menu items (e.g., File, View, Tools) with optional children.
/// </summary>
public interface IMenuModelContribution
{
    /// <summary>
    /// Builds the hierarchical menu model exposed by this plugin.
    /// </summary>
    /// <returns>Root-level menu item descriptors, each possibly containing children.</returns>
    IEnumerable<MenuItemDescriptor> BuildMenuModel(); // root-level items (e.g., File, View, Tools)
}