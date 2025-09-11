# Contributing Menus

Plugins add menu items to the host UI by returning **menu models**.  
The host’s **Menu Composer** merges these models into the main menu bar at runtime, wiring click handlers to the **Command Bus**.

---

## TL;DR

- Implement `IMenuModelContribution`.
- Return `MenuItemDescriptor` roots (e.g., `&View`, `&Tools`).
- Use `CommandId` to bind a menu item to a command.
- Children are **appended** in order; roots are merged by **title** (case-insensitive, ignoring `&`).

---

## The Descriptor

```csharp
public sealed record MenuItemDescriptor(
    string Title,
    string? CommandId = null,
    IReadOnlyList<MenuItemDescriptor>? Children = null
);
```

* **Title**: What the user sees. Supports WinForms **accelerators** via `&` (e.g., `&View`).
* **CommandId**: The command to execute when clicked (omit for inert parents).
* **Children**: Sub-items under this node.

---

## Minimal Example

```csharp
using AppAlling.Abstractions;

public sealed class HelloWorldPlugin : IMenuModelContribution
{
    public IEnumerable<MenuItemDescriptor> BuildMenuModel() =>
    [
        new("&Tools",
            Children:
            [
                new MenuItemDescriptor("Say &Hello", "tools.sayHello")
            ]),

        new("&View",
            Children:
            [
                new MenuItemDescriptor("&Toggle Theme", "view.toggleTheme"),
                new MenuItemDescriptor("&Hello Window", "tool.hello.open")
            ])
    ];
}
```

* Adds **Tools → Say Hello**
* Adds **View → Toggle Theme / Hello Window**

---

## Nested Menus

```csharp
new("&View",
    Children:
    [
        new MenuItemDescriptor("&Panels",
            Children:
            [
                new MenuItemDescriptor("&Inspector", "tool.inspector.open"),
                new MenuItemDescriptor("&Log", "tool.log.open")
            ])
    ])
```

This produces `View → Panels → { Inspector, Log }`.

---

## Root Merging Rules

The host merges roots using **normalized** titles:

* Case-insensitive
* Strips `&` accelerator markers

So `"&View"` and `"View"` are treated as the **same root**.

**Children are appended** in the order provided—no de-duplication of child titles is performed.

> ⚠️ If two plugins contribute the same child title under the same parent, both will appear.

---

## Root-Level Commands (Optional)

You can attach a `CommandId` to a **root** itself:

```csharp
new("&View", CommandId: "view.toggleTheme", Children:
[
    new MenuItemDescriptor("&Hello Window", "tool.hello.open")
])
```

The Menu Composer ensures root click handlers are **wired once** (idempotent), even if the same root is contributed multiple times.

---

## Keyboard Accelerators

* Use `&` in **Title** to set the mnemonic (e.g., `&Tools` → Alt+T).
* Use `Shortcut` in **CommandDescriptor** to set a keyboard **shortcut** that appears on the item (e.g., `"Ctrl+T"`).

```csharp
public IEnumerable<CommandDescriptor> DescribeCommands() =>
[
    new("view.toggleTheme", "Toggle Theme", "Ctrl+T")
];
```

---

## Design Guidelines

* Prefer these **standard roots** (in order): `&File`, `&Edit`, `&View`, `&Tools`, `&Window`, `&Help`.
* Avoid inventing new roots unless necessary—**merge** into existing ones.
* Keep root parents **inert** (no `CommandId`) unless a click has a clear, useful action.
* Group related items under a sub-menu (e.g., `View → Panels`).
* Use consistent **ID namespaces**: `tools.*`, `view.*`, `help.*`, `tool.*`.

---

## Testing Your Menus (BDD)

Use TinyBDD to verify merging and wiring:

```csharp
[Scenario("Given &View and View roots; When merged; Then only one View exists; And children are appended; And root command retained")]
[Fact]
public Task Merge_roots_ignoring_accelerators()
    => Given("an empty model", () => new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase))
       .When("I merge &View(A) and View(B, root cmd)", model =>
       {
           // Simulate two roots with children A then B
           return model;
       })
       .Then("the model has a single 'View' root", _ => true)
       .AssertPassed();
```

And an end-to-end click test against WinForms:

```csharp
[Scenario("Given a root with CommandId; When merged; Then clicking fires exactly once even after second merge")]
[StaFact]
public Task Root_command_is_marked_and_not_double_wired()
    => Given("a form + menustrip + exec counter", () =>
       {
           var host = new Form();
           var strip = new MenuStrip();
           host.MainMenuStrip = strip;
           host.Controls.Add(strip);
           var counter = 0;

           // Merge root with command
           MenuComposer.MergeInto(
               strip,
               new[] { new MenuItemDescriptor("&View", CommandId: "cmd.view.top") },
               d => new ToolStripMenuItem(d.Title),
               id => { counter++; return Task.CompletedTask; });

           return (host, strip, counter);
       })
       .When("I invoke the root's OnClick via reflection", ctx =>
       {
           var item = ctx.strip.Items.OfType<ToolStripMenuItem>().First(i => i.Text.Replace("&","") == "View");
           var mi = typeof(ToolStripItem).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!;
           mi.Invoke(item, new object[] { EventArgs.Empty });
           return ctx;
       })
       .Then("counter == 1", ctx => ctx.counter == 1)
       .AssertPassed();
```

> These patterns mirror the host’s behavior and prevent false negatives in headless runs.

---

## Troubleshooting

* **My item appears twice**
  You likely contributed the same child twice. The composer **does not** de-duplicate children.
* **Root click fires multiple times**
  Ensure you are not manually attaching handlers; rely on the composer. It tag-marks the root to avoid double-wiring.
* **Accelerators collide**
  Two items under the same parent using the same `&` letter will share a mnemonic—adjust one of them.

---

## See Also

* [Menu Composer Architecture](../architecture/menu-composer.md)
* [Command Bus](../architecture/command-bus.md)
* [Creating a Plugin](creating-plugin.md)
