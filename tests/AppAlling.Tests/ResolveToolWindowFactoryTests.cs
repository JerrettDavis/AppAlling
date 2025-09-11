using System.Windows.Forms;
using AppAlling.UI.WinForms.Commands;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("CommandRegistry.ResolveToolWindowFactory – behavior")]
public class ResolveToolWindowFactoryTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // A simple test factory we can register into DI.
    private sealed class DemoFactory : IToolWindowFactory
    {
        public string CommandId => "tool.demo.open";
        public int CreatedCount { get; private set; }
        public Form Create()
        {
            CreatedCount++;
            return new Form { Text = "Demo Tool" };
        }
    }

    // ---------- Helpers ----------
    private static (ServiceProvider SP, DemoFactory Factory) Given_services_with_demo_factory()
    {
        var services = new ServiceCollection();
        var factory = new DemoFactory();
        services.AddSingleton<IToolWindowFactory>(factory);
        var sp = services.BuildServiceProvider();
        return (sp, factory);
    }

    private static (ServiceProvider SP, DemoFactory Factory, Func<Form> Creator)
        When_I_resolve_creator_for((ServiceProvider SP, DemoFactory Factory) ctx, string commandId)
    {
        var creator = CommandRegistry.ResolveToolWindowFactory(ctx.SP, commandId);
        return (ctx.SP, ctx.Factory, creator);
    }

    private static (ServiceProvider SP, DemoFactory Factory, Form First, Form Second)
        And_I_create_two_forms((ServiceProvider SP, DemoFactory Factory, Func<Form> Creator) ctx)
    {
        var f1 = ctx.Creator();
        var f2 = ctx.Creator();
        return (ctx.SP, ctx.Factory, f1, f2);
    }

    private static bool Then_both_forms_are_new_instances_and_titled((ServiceProvider SP, DemoFactory Factory, Form First, Form Second) ctx, string expectedTitle)
        => ctx.First != null
           && ctx.Second != null
           && ctx.First != ctx.Second
           && ctx.First.Text == expectedTitle
           && ctx.Second.Text == expectedTitle;

    private static bool Then_factory_Create_was_called(DemoFactory f, int expectedAtLeast)
        => f.CreatedCount >= expectedAtLeast;

    private static (ServiceProvider SP, Func<Form> Creator) Given_services_without_factories()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        return (sp, null!);
    }

    private static (ServiceProvider SP, Form Fallback) When_I_resolve_missing_and_create((ServiceProvider SP, Func<Form> Creator) ctx, string id)
    {
        var creator = CommandRegistry.ResolveToolWindowFactory(ctx.SP, id);
        return (ctx.SP, creator());
    }

    private static bool Then_fallback_form_has_expected_text(Form f, string id)
        => f is not null && f.Text == $"Missing tool window for '{id}'";

    // ---------- Scenarios ----------

    [Scenario("Given services with a DemoFactory; When I resolve 'tool.demo.open'; Then Create() yields new forms titled 'Demo Tool' and factory was called")]
    [StaFact]
    public Task Resolves_registered_factory_and_creates_forms()
        => Given("services with DemoFactory", Given_services_with_demo_factory)
           .When("I resolve creator for 'tool.demo.open'", ctx => When_I_resolve_creator_for(ctx, "tool.demo.open"))
           .And("I create two forms from that creator", And_I_create_two_forms)
           .Then("both instances are distinct and titled 'Demo Tool'", ctx => Then_both_forms_are_new_instances_and_titled(ctx, "Demo Tool"))
           .And("the factory Create() was called at least twice", ctx => Then_factory_Create_was_called(ctx.Factory, 2))
           .AssertPassed();

    [Scenario("Given services with a DemoFactory; When I resolve using different casing; Then it still resolves and creates forms")]
    [StaFact]
    public Task Resolution_is_case_insensitive()
        => Given("services with DemoFactory", Given_services_with_demo_factory)
           .When("I resolve creator for 'TOOL.DEMO.OPEN' (upper)", ctx => When_I_resolve_creator_for(ctx, "TOOL.DEMO.OPEN"))
           .And("I create two forms from that creator", And_I_create_two_forms)
           .Then("both instances are distinct and titled 'Demo Tool'", ctx => Then_both_forms_are_new_instances_and_titled(ctx, "Demo Tool"))
           .And("factory Create() was called at least twice", ctx => Then_factory_Create_was_called(ctx.Factory, 2))
           .AssertPassed();

    [Scenario("Given services without factories; When I resolve an unknown id; Then the fallback Form is returned with the expected text")]
    [StaFact]
    public Task Missing_id_returns_fallback_form()
        => Given("an empty service provider", Given_services_without_factories)
           .When("I resolve missing id 'tool.unknown.open' and create", ctx => When_I_resolve_missing_and_create(ctx, "tool.unknown.open"))
           .Then("the fallback form text matches the id", ctx => Then_fallback_form_has_expected_text(ctx.Fallback, "tool.unknown.open"))
           .AssertPassed();
}
