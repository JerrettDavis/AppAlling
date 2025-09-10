using AppAlling.Application;
using AppAlling.PluginHost;
using AppAlling.UI.WinForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DependencyInjection = AppAlling.UI.WinForms.DependencyInjection;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

ApplicationConfiguration.Initialize();

var configuration = DependencyInjection.BuildConfiguration();
var appSettings = configuration.Get<AppSettings>();
var services = new ServiceCollection()
    .AddAppAllingApplication()
    .AddAppAllingUiWinForms(configuration)
    .AddTransient<MainForm>();

var loader = new PluginLoader(appSettings?.Plugins.Directory ?? "Plugins");
var plugins = loader.LoadPlugins(out var contexts);

PluginLoader.ConfigureAll(services, plugins, contexts);

var provider = services.BuildServiceProvider();

Application.Run(provider.GetRequiredService<MainForm>());