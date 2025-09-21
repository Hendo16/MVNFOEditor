using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Config.Net;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MVNFOEditor.Common;
using MVNFOEditor.DB;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Services;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor;

public partial class App : Application
{
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private static MusicDbContext _dbContext;
    private static MusicDBHelper _dbHelper;
    private static YTDLHelper _ytdlHelper;
    private static YTMusicHelper _ytmHelper;
    private static iTunesAPIHelper _iTunesHelper;
    private static AppleMusicDLHelper _appleMusicDLHelper;
    private static DefaultViewModel _mainViewModel;
    private static IDataTemplate _viewLocater;
    private static ISukiToastManager _toastManager;
    private static ISukiDialogManager _dialogManager;
    private static ISettings _settings;
    private IServiceProvider? _provider;
    private bool _enableAppleMusic = true;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _provider = ConfigureServices();
        _settings = new ConfigurationBuilder<ISettings>()
            .UseJsonFile("./Assets/config.json")
            .Build();
        _dbContext = new MusicDbContext();
        _toastManager = new SukiToastManager();
        _dialogManager = new SukiDialogManager();
        ConfigureLogging();
        SetupHelpers();
    }
    
    private void ConfigureLogging()
    {
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        var configFileInfo = new FileInfo("log4net.config");
        XmlConfigurator.Configure(logRepository, configFileInfo);
    }

    private async void SetupHelpers()
    {
        _dbHelper = MusicDBHelper.CreateHelper(_dbContext);
        _ytdlHelper = YTDLHelper.CreateHelper();
        _iTunesHelper = iTunesAPIHelper.CreateHelper();
        _ytmHelper = await YTMusicHelper.CreateHelper();
        if (_enableAppleMusic)
        {
            _appleMusicDLHelper = await AppleMusicDLHelper.CreateHelper();
        }
        CheckSettings();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewLocator = _provider?.GetRequiredService<IDataTemplate>();
            _viewLocater = viewLocator;
            var mainVm = _provider?.GetRequiredService<DefaultViewModel>();
            _mainViewModel = mainVm;
            desktop.MainWindow = viewLocator?.Build(mainVm) as Window;
        }

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        base.OnFrameworkInitializationCompleted();
    }
    private async void CheckSettings()
    {
        if (!_dbContext.Exists())
        {
            SettingsDialogViewModel newDialog = new SettingsDialogViewModel();
            GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => newDialog)
                .TryShow();
        }
        //AM Check
        if (_enableAppleMusic && !_appleMusicDLHelper.IsValidToken())
        {
            GetVM().GetDialogManager().CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle("Apple Music Token Expired")
                .WithViewModel(dialog => new AMUserSubmissionViewModel(dialog))
                .TryShow();
        }
        
        //YTDL/FFMPEG Check
        #if WINDOWS
        if (!File.Exists($"{_settings.YTDLPath}/yt-dlp.exe"))
        {
            Console.WriteLine("Downloading YT-DLP...");
            await YoutubeDLSharp.Utils.DownloadYtDlp(_settings.YTDLPath);
        }
        if (!File.Exists($"{_settings.FFMPEGPath}/ffmpeg.exe"))
        {  
            Console.WriteLine("Downloading FFMPEG...");
            await YoutubeDLSharp.Utils.DownloadFFmpeg(_settings.FFMPEGPath);
        }
        if (!File.Exists($"{_settings.FFMPEGPath}/ffprobe.exe"))
        {  
            Console.WriteLine("Downloading FFPROBE...");
            await YoutubeDLSharp.Utils.DownloadFFprobe(_settings.FFPROBEPath);
        }
        #endif
    }

    private static ServiceProvider ConfigureServices()
    {
        var viewlocator = Current?.DataTemplates.First(x => x is ViewLocator);
        var services = new ServiceCollection();

        if (viewlocator is not null)
            services.AddSingleton(viewlocator);
        services.AddSingleton<PageNavigationService>();

        // Viewmodels
        services.AddSingleton<DefaultViewModel>();
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => !p.IsAbstract && typeof(PageBase).IsAssignableFrom(p));
        foreach (var type in types)
            services.AddSingleton(typeof(PageBase), type);

        return services.BuildServiceProvider();
    } 

    public static ISettings GetSettings()
    {
        return _settings;
    }

    public static MusicDbContext GetDBContext()
    {
        return _dbContext;
    }

    public static MusicDBHelper GetDBHelper()
    {
        return _dbHelper;
    }

    public static YTDLHelper GetYTDLHelper()
    {
        return _ytdlHelper;
    }

    public static async void RefreshYTMusicHelper()
    {
        _ytmHelper = await YTMusicHelper.CreateHelper("./Assets/browser.json");
    }

    public static YTMusicHelper GetYTMusicHelper()
    {
        return _ytmHelper;
    }

    public static iTunesAPIHelper GetiTunesHelper()
    {
        return _iTunesHelper;
    }

    public static AppleMusicDLHelper GetAppleMusicDLHelper()
    {
        return _appleMusicDLHelper;
    }

    public static DefaultViewModel GetVM()
    {
        return _mainViewModel;
    }

    public static ISukiToastManager GetToastManager()
    {
        return _toastManager;
    }

    public static ISukiDialogManager GetDialogManager()
    {
        return _dialogManager;
    }
}