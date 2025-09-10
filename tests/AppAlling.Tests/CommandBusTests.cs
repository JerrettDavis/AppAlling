using AppAlling.Abstractions;
using AppAlling.Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Tests;

[Feature("Command Bus – behavior-driven acceptance")]
public class CommandBusTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ---- Shared models for tests ---------------------------------------------------------

    private sealed class Counter
    {
        public int Value;
    }

    private sealed class CounterSvc
    {
        public int Value;
    }

    // A tiny context tuple alias to keep types readable in step lambdas
    private readonly record struct BusAndHolder(CommandBus Bus, Counter Holder);

    private readonly record struct BusAndProvider(CommandBus Bus, ServiceProvider Provider);

    // ---- Step helpers (reusable across scenarios) ---------------------------------------


    // GIVEN a command bus and a mutable counter holder
    private static BusAndHolder Given_a_command_bus_and_a_counter_holder()
        => new(new CommandBus(), new Counter());

    // GIVEN services with a counter service and a command bus
    private static BusAndProvider Given_services_with_counter_service_and_a_command_bus()
    {
        var services = new ServiceCollection().AddSingleton<CounterSvc>();
        return new(new CommandBus(), services.BuildServiceProvider());
    }

    // AND the 'demo.increment' command increments the mutable holder
    private static BusAndHolder When_the_demo_increment_increments_holder(BusAndHolder ctx)
    {
        ctx.Bus.Register(new CommandDescriptor("demo.increment", "Increment"), _ =>
        {
            Interlocked.Increment(ref ctx.Holder.Value);
            return Task.CompletedTask;
        });
        return ctx;
    }

    // AND the 'demo.increment' command increments the DI service
    private static BusAndProvider When_the_demo_increment_increments_service(BusAndProvider ctx)
    {
        ctx.Bus.Register(new CommandDescriptor("demo.increment", "Increment"), sp =>
        {
            sp.GetRequiredService<CounterSvc>().Value++;
            return Task.CompletedTask;
        });
        return ctx;
    }

    // WHEN I execute 'demo.increment' once (DI)
    private static async Task<BusAndProvider> When_I_execute_demo_increment_once(BusAndProvider ctx)
    {
        await ctx.Bus.ExecuteAsync("demo.increment", ctx.Provider);
        return ctx;
    }

    // WHEN I execute 'demo.increment' once (no DI usage needed)
    private static async Task<BusAndHolder> When_I_execute_demo_increment_once(BusAndHolder ctx)
    {
        // For commands that don't need services, we can pass an empty provider
        var sp = new ServiceCollection().BuildServiceProvider();
        await ctx.Bus.ExecuteAsync("demo.increment", sp);
        return ctx;
    }

    // THEN the holder counter should equal {expected}
    private static bool Then_the_holder_counter_should_equal(BusAndHolder ctx, int expected)
        => ctx.Holder.Value == expected;

    // THEN the service counter should equal {expected}
    private static bool Then_the_service_counter_should_equal(BusAndProvider ctx, int expected)
        => ctx.Provider.GetRequiredService<CounterSvc>().Value == expected;

    // ---- Scenarios ----------------------------------------------------------------------

    [Scenario("Given a command bus and a counter holder; When I execute 'demo.increment'; Then the counter equals 1")]
    [Fact]
    public Task Holder_is_incremented_by_registered_command()
        => Given("a command bus and a counter holder", Given_a_command_bus_and_a_counter_holder)
            .When("the 'demo.increment' command increments the holder", When_the_demo_increment_increments_holder)
            .And("I execute 'demo.increment' once", When_I_execute_demo_increment_once)
            .Then("the holder counter should equal 1", ctx => Then_the_holder_counter_should_equal(ctx, 1))
            .AssertPassed(TestContext.Current.CancellationToken);

    [Scenario("Given services with a counter service and a command bus; When I execute 'demo.increment'; Then the service counter equals 1")]
    [Fact]
    public Task Service_is_incremented_via_DI()
        => Given("services with a counter service and a command bus", Given_services_with_counter_service_and_a_command_bus)
            .When("the 'demo.increment' command increments the service", When_the_demo_increment_increments_service)
            .And("I execute 'demo.increment' once", When_I_execute_demo_increment_once)
            .Then("the service counter should equal 1", ctx => Then_the_service_counter_should_equal(ctx, 1))
            .AssertPassed(TestContext.Current.CancellationToken);


    [Scenario("Given a fresh counter; And two registrations for the same id; When I execute it; Then the latest registration wins")]
    [Fact]
    public Task Last_write_wins_on_duplicate_registration()
    {
        return Given("a fresh command bus and a counter", Given_a_command_bus_and_a_counter_holder)
            .When("I register 'demo.toggle' to set result = 1", ctx =>
            {
                var cmd = new CommandDescriptor("demo.toggle", "Toggle");
                ctx.Bus.Register(cmd, _ =>
                {
                    Volatile.Write(ref ctx.Holder.Value, 1);
                    return Task.CompletedTask;
                });
                return ctx;
            })
            .And("I re-register 'demo.toggle' to set result = 2", ctx =>
            {
                var cmd = new CommandDescriptor("demo.toggle", "Toggle");
                ctx.Bus.Register(cmd, _ =>
                {
                    Volatile.Write(ref ctx.Holder.Value, 2);
                    return Task.CompletedTask;
                });
                return ctx;
            })
            .And("I execute 'demo.toggle'", async Task<BusAndHolder> (ctx) =>
            {
                var sp = new ServiceCollection().BuildServiceProvider();
                await ctx.Bus.ExecuteAsync("demo.toggle", sp);
                return ctx;
            })
            .Then("the latest handler ran (result == 2)", ctx => Then_the_holder_counter_should_equal(ctx, 2))
            .AssertPassed(TestContext.Current.CancellationToken);
    }
}