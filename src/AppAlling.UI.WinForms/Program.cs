using AppAlling.UI.WinForms;
using DependencyInjection = AppAlling.UI.WinForms.DependencyInjection;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
ApplicationConfiguration.Initialize();

var configuration = DependencyInjection.BuildConfiguration();
var (_, form) = Bootstrap.Build(configuration);

Application.Run(form);