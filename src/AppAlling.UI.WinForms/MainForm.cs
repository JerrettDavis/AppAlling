using System.Reactive.Linq;
using AppAlling.Abstractions;
using AppAlling.Abstractions.State;
using AppAlling.Application.Commands;
using AppAlling.Application.State;
using AppAlling.UI.WinForms.Commands;

namespace AppAlling.UI.WinForms;

/// <summary>
/// The main application window that composes commands, menus, and tool windows from plugins
/// and reacts to application state changes (e.g., theme) using the reactive store.
/// </summary>
public partial class MainForm : Form
{
    private readonly IServiceProvider _services;
    private readonly ICommandBus _bus;

    /// <summary>
    /// Creates a new instance of the main window and wires up DI-provided contributions.
    /// </summary>
    public MainForm(
        IServiceProvider services,
        ICommandBus bus,
        IEnumerable<ICommandContribution> commands,
        IEnumerable<IMenuModelContribution> menus,
        IEnumerable<IToolWindowContribution> tools,
        IStore<AppState> store)
    {
        InitializeComponent();
        _services = services;
        _bus = bus;

        // 1) Register commands
        RegisterCommands(commands, tools);

        // 2) Build Menus from model
        BuildMenus(menus);

        // 3) React to state (theme, etc.)
        store.States
            .Select(s => s.Theme)
            .DistinctUntilChanged()
            .Subscribe(ApplyTheme);
        ApplyTheme(store.Current.Theme);
    }

    private void RegisterCommands(
        IEnumerable<ICommandContribution> commands,
        IEnumerable<IToolWindowContribution> tools)
    {
        // Plugin-defined commands
        foreach (var cc in commands)
        {
            foreach (var c in cc.DescribeCommands())
            {
                // Ask the plugin for execution delegate if it exposed it via DI.
                // Convention: a transient ICommandHandler<MyId>? If you like, introduce an ICommandHandler<TId>.
                // For now, use a naming convention hook point plugins can register:
                var exec = CommandRegistry.ResolveExecuteDelegate(_services, c.Id);
                _bus.Register(c, exec);
            }
        }

        // Tool windows get implicit "open" commands
        foreach (var tw in tools.SelectMany(t => t.DescribeToolWindows()))
        {
            var desc = new CommandDescriptor(
                Id: tw.CommandId,
                Title: $"Open {tw.Title}"
            );

            _bus.Register(desc, async sp =>
            {
                // each plugin should register a factory for their window under this command id
                var factory = CommandRegistry.ResolveToolWindowFactory(sp, tw.CommandId);
                var form = factory();
                form.Show(this);
                await Task.CompletedTask;
            });
        }
    }

    private void BuildMenus(IEnumerable<IMenuModelContribution> menus)
    {
        var roots = menus.SelectMany(m => m.BuildMenuModel());
        MenuComposer.MergeInto(
            menuStrip1,
            roots,
            BuildMenuItem,
            id => _bus.ExecuteAsync(id, _services)
        );
    }

    private ToolStripMenuItem BuildMenuItem(MenuItemDescriptor d)
    {
        var item = new ToolStripMenuItem(d.Title);

        if (d.CommandId is { } id)
        {
            var info = _bus.List.GetValueOrDefault(id);
            var keys = ShortcutParser.Parse(info?.Shortcut);
            if (keys is not null) item.ShortcutKeys = keys.Value;

            item.Click += async (_, _) => await _bus.ExecuteAsync(id, _services);
        }

        if (d.Children is null)
            return item;

        foreach (var child in d.Children)
            item.DropDownItems.Add(BuildMenuItem(child));

        return item;
    }

    private void ApplyTheme(string theme)
    {
        var dark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        BackColor = dark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        ForeColor = dark ? Color.WhiteSmoke : SystemColors.ControlText;
        foreach (Control c in Controls)
        {
            c.BackColor = BackColor;
            c.ForeColor = ForeColor;
        }

        statusLabel.Text = $"Theme: {theme}";
    }
}