# Menu Composer

The **Menu Composer** is responsible for dynamically constructing the application's menu structure at runtime.  
It takes menu models contributed by plugins, merges them with existing menu roots, and wires up command execution in a safe and idempotent way.

---

## Goals

- **Dynamic composition** – Plugins can add menus without modifying the main form.
- **Idempotent wiring** – Re-merging the same root should not double-wire click handlers.
- **Keyboard accelerators** – Menu titles can include `&` markers, which are ignored for matching.

---

## API

```csharp
public static class MenuComposer
{
    public static void MergeInto(
        MenuStrip menuStrip,
        IEnumerable<MenuItemDescriptor> roots,
        Func<MenuItemDescriptor, ToolStripMenuItem> buildItem,
        Func<string, Task> executeAsync);
}
```

* **menuStrip** – Target WinForms `MenuStrip` to modify.
* **roots** – Plugin-provided menu descriptors.
* **buildItem** – Factory function to create child `ToolStripMenuItem`s from descriptors.
* **executeAsync** – Delegate invoked when a root with a `CommandId` is clicked.

---

## Root Matching

Root menu items are matched **case-insensitively** and with all `&` accelerators removed:

```csharp
private static string NormalizeTitle(string s)
    => s.Replace("&", "", StringComparison.Ordinal);
```

This allows a plugin to contribute `"&View"` while another plugin contributes `"View"` — both resolve to the same root.

---

## Root Command Handling

Some root menus may have a `CommandId`.
`MenuComposer` ensures a **single click handler** is registered:

```csharp
private const string RootCmdTagPrefix = "__root_cmd__:";

private static bool HasRootCommand(ToolStripMenuItem item)
    => (item.Tag as string)?.StartsWith(RootCmdTagPrefix, StringComparison.Ordinal) == true;

private static void MarkRootCommand(ToolStripMenuItem item, string id)
    => item.Tag = RootCmdTagPrefix + id;
```

If a root already has a command marker, re-merging will **not** attach a second event handler — avoiding duplicate execution.

---

## Example

Given these plugin menu models:

```csharp
new("&View", Children:
[
    new MenuItemDescriptor("A", "cmd.a")
]),
new("View", CommandId: "cmd.view.top", Children:
[
    new MenuItemDescriptor("B", "cmd.b")
])
```

The resulting menu will contain a single `View` root with:

* **Root click handler** → executes `cmd.view.top`
* **Child items** → `A`, `B` in order

---

## Behavioral Guarantees

* **No duplicate roots** – Matching is performed by normalized title.
* **Stable click handlers** – Even after multiple calls to `MergeInto`, each root fires exactly once per click.
* **Children append only** – New children are always appended, no de-duplication of children is performed (same as current host behavior).

---

## Testing

This logic is fully covered by behavior-driven tests:

* **Root Merging** – Ensures `&View` + `View` are coalesced.
* **Command Wiring** – Verifies that clicking fires exactly once per click, even after re-merge.
* **Unmarked Roots** – Roots without a command remain inert.

```csharp
[Scenario("Given a root with CommandId; When merged; Then the top is marked and clicking fires exactly once per click even after a second merge")]
public Task Root_command_is_marked_and_not_double_wired() { ... }
```

---

## Best Practices

* Prefer **children under existing roots** rather than creating new roots whenever possible (keeps menu bar clean).
* Assign **meaningful CommandIds** so that hotkey bindings and tests can reference them.
* Keep click handlers lightweight; defer heavy work to DI-resolved services.

---

## Related Topics

* [Command Bus](command-bus.md)
* [State Store](state-store.md)
* [Architecture Overview](overview.md)

