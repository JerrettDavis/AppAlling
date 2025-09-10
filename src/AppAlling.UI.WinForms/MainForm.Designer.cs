namespace AppAlling.UI.WinForms;
 partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private MenuStrip menuStrip1;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem exitItem;
    private ToolStripMenuItem viewMenu;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip1 = new MenuStrip();
        fileMenu = new ToolStripMenuItem("&File");
        exitItem = new ToolStripMenuItem("E&xit");
        viewMenu = new ToolStripMenuItem("&View");
        statusStrip1 = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Ready");

        SuspendLayout();

        menuStrip1.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu });
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { exitItem });

        exitItem.Click += (_, __) => Close();

        statusStrip1.Items.Add(statusLabel);

        Controls.Add(menuStrip1);
        Controls.Add(statusStrip1);

        MainMenuStrip = menuStrip1;
        Text = "AppAlling";
        Width = 900; Height = 600;

        ResumeLayout(false);
        PerformLayout();
    }
}
