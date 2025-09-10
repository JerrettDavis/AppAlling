using AppAlling.Abstractions;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("Menu composition – root merging and child append (pure)")]
public class MenuComposerAcceptanceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ---------- Pure composer with same semantics as shell ----------
    private static string Normalize(string s) => s.Replace("&", "", StringComparison.Ordinal);

    private sealed record RootBag(string Title, string? CommandId, List<MenuItemDescriptor> Children);

    private static Dictionary<string, RootBag> Given_an_empty_menu_model()
        => new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, RootBag> When_I_merge_roots(
        Dictionary<string, RootBag> model,
        IEnumerable<MenuItemDescriptor> roots)
    {
        foreach (var root in roots)
        {
            var key = Normalize(root.Title);
            if (!model.TryGetValue(key, out var bag))
            {
                bag = new RootBag(root.Title, root.CommandId, []);
                model[key] = bag;
            }
            else
            {
                // keep original Title casing; keep existing CommandId if already present
                if (bag.CommandId is null && root.CommandId is { } id) bag = model[key] = bag with { CommandId = id };
            }

            if (root.Children is { } kids)
                bag.Children.AddRange(kids); // append, no de-dupe
        }

        return model;
    }

    private static IEnumerable<MenuItemDescriptor> Given_two_view_roots_with_children()
        =>
        [
            new("&View", Children:
            [
                new MenuItemDescriptor("A", "cmd.a")
            ]),
            new("View", CommandId: "cmd.view.top", Children:
            [
                new MenuItemDescriptor("B", "cmd.b")
            ])
        ];

    private static bool Then_model_has_single_root_named_view(Dictionary<string, RootBag> model)
        => model.Count == 1 && model.ContainsKey("View");

    private static bool And_children_are_appended_in_order(Dictionary<string, RootBag> model)
        => model["View"].Children.Select(c => c.Title).SequenceEqual(["A", "B"]);

    private static bool And_root_command_is_retained(Dictionary<string, RootBag> model)
        => model["View"].CommandId == "cmd.view.top";

    // ---------- Scenarios ----------
    [Scenario("Given &View and View roots; When merged; Then only one View exists; And children appended; And root command retained")]
    [Fact]
    public Task Merge_roots_ignoring_accelerators()
        => Given("an empty menu model", Given_an_empty_menu_model)
            .When("I merge &View and View roots with children", model => When_I_merge_roots(model, Given_two_view_roots_with_children()))
            .Then("the model has a single 'View' root", Then_model_has_single_root_named_view)
            .And("its children are appended in order (A, B)", And_children_are_appended_in_order)
            .And("the root command id is retained if present", And_root_command_is_retained)
            .AssertPassed(TestContext.Current.CancellationToken);
}