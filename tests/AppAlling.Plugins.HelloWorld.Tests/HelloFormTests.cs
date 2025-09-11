using System.Windows.Forms;
using TinyBDD;
using TinyBDD.Xunit.v3;

namespace AppAlling.Plugins.HelloWorld.Tests;

[Feature("HelloForm – plugin tool window contract")]
public class HelloFormTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Given a HelloForm; Then it has the correct title, size, and label")]
    [StaFact] // ensure STA thread for WinForms
    public Task HelloForm_properties_are_correct()
        => Given("a new HelloForm", () => new HelloForm())
            .Then("its title should be 'Hello Window'", form => form.Text == "Hello Window")
            .And("its width should be 260 and height 120", form => form.Width == 260 && form.Height == 120)
            .And("it contains exactly one Label", form => form.Controls.OfType<Label>().Count() == 1)
            .And("that label has the expected text and position", form =>
            {
                var lbl = form.Controls.OfType<Label>().First();
                return lbl.Text == "Hello from plugin!" &&
                       lbl is { AutoSize: true, Left: 10, Top: 10 };
            })
            .AssertPassed();
}