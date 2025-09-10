using System.Windows.Forms;

namespace AppAlling.Plugins.HelloWorld;

/// <summary>
/// Simple tool window displayed by the HelloWorld plugin.
/// </summary>
public sealed class HelloForm : Form
{
    public HelloForm()
    {
        Text = "Hello Window";
        var label = new Label { AutoSize = true, Text = "Hello from plugin!", Left = 10, Top = 10 };
        Controls.Add(label);
        Width = 260;
        Height = 120;
    }
}