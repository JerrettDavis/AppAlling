using System.Windows.Forms;

namespace AppAlling.Tests.Support;

public sealed class TestAppContext : ApplicationContext
{
    public TestAppContext(Form main)
    {
        // Show the form (not strictly required to click menu items, but realistic)
        MainForm = main;
        main.FormClosed += (_, __) => ExitThread();
        main.Show();
    }

    public void Quit() => ExitThread();
}