using System.Windows.Forms;
using AppAlling.Abstractions;

namespace AppAlling.UI.WinForms;

/// <summary>
/// Composes menu roots into an existing <see cref="MenuStrip"/>.
/// Matches titles case-insensitively and ignores '&amp;' accelerators, just like the inlined logic.
/// </summary>
internal static class MenuComposer
{
    /// <summary>
    /// Merge plugin-provided root menu descriptors into an existing MenuStrip.
    /// Respects the following rules:
    /// - If a top-level title already exists (ignoring '&amp;'), reuse it; otherwise create a new item.
    /// - If a root has a CommandId, attach a single click handler to execute it (only once per root).
    /// - Append all children in order; no de-duplication is performed (matches current behavior).
    /// </summary>
    public static void MergeInto(
        MenuStrip menuStrip,
        IEnumerable<MenuItemDescriptor> roots,
        Func<MenuItemDescriptor, ToolStripMenuItem> buildItem,
        Func<string, Task> executeAsync)
    {
        foreach (var root in roots)
        {
            var top = EnsureTopMenu(menuStrip, root.Title);

            // Top-level commands are rare but supported: wire once.
            if (root.CommandId is { } topCmd && !HasRootCommand(top))
            {
                MarkRootCommand(top, topCmd);
                top.Click += async (_, __) => await executeAsync(topCmd);
            }

            if (root.Children is null) continue;

            foreach (var child in root.Children)
            {
                var childItem = buildItem(child);
                top.DropDownItems.Add(childItem);
            }
        }
    }

    private static ToolStripMenuItem EnsureTopMenu(MenuStrip strip, string title)
    {
        var norm = NormalizeTitle(title);
        foreach (ToolStripMenuItem i in strip.Items)
            if (NormalizeTitle(i.Text ?? string.Empty) == norm)
                return i;

        var created = new ToolStripMenuItem(title);
        strip.Items.Add(created);
        return created;
    }

    private static string NormalizeTitle(string s) => s.Replace("&", "", StringComparison.Ordinal);

    // We stash the wired command id in Tag to avoid duplicate event handlers
    private const string RootCmdTagPrefix = "__root_cmd__:";

    private static bool HasRootCommand(ToolStripMenuItem item)
        => (item.Tag as string)?.StartsWith(RootCmdTagPrefix, StringComparison.Ordinal) == true;

    private static void MarkRootCommand(ToolStripMenuItem item, string id)
        => item.Tag = RootCmdTagPrefix + id;
}