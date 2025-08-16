using System;
using System.IO;
using System.Net;
using System.Reflection;
using Avalonia;
using Avalonia.Dialogs;
using Config.Net;
using Flurl.Http;
using log4net;
using log4net.Config;

namespace MVNFOEditor.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    
    private static void ConfigureLogging()
    {
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly() ?? throw new InvalidOperationException());
        var configFileInfo = new FileInfo("log4net.config");
        XmlConfigurator.Configure(logRepository, configFileInfo);
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        ConfigureLogging();
        var app = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
        
        /*
        FlurlHttp.Clients.WithDefaults(builder => builder
            .ConfigureInnerHandler(hch => {
                hch.Proxy = new WebProxy("http://localhost:8000");
                hch.UseProxy = true;
            }));
        */
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();
        return app;
    }
}
