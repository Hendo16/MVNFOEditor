using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.ViewModels;
using MVNFOEditor.Views;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.ComponentModel;
using System.Linq;
using MVNFOEditor.Common;
using MVNFOEditor.Features;
using MVNFOEditor.Services;
using Microsoft.Extensions.Hosting.Internal;
using MVNFOEditor.Models;
using SukiUI.Controls;

namespace MVNFOEditor;

public partial class App : Application
{
    private static MusicDbContext _dbContext;
    private static MusicDBHelper _dbHelper;
    private static YTDLHelper _ytdlHelper;
    private static YTMusicHelper _ytmHelper;
    private static DefaultViewModel _mainViewModel;
    private static SettingsData _settingsData;
    private static IDataTemplate _viewLocater;
    private IServiceProvider? _provider;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _provider = ConfigureServices();
        _dbContext = new MusicDbContext();
        _dbContext.Database.EnsureCreated();
        _ytmHelper = new YTMusicHelper();
        _dbHelper = new MusicDBHelper(_dbContext);
        _ytdlHelper = new YTDLHelper();
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
            desktop.MainWindow.Opened += CheckSettings;
        }

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        base.OnFrameworkInitializationCompleted();
    }
    private void CheckSettings(object sender, System.EventArgs e)
    {
        if (!GetDBHelper().CheckIfSettingsValid())
        {
            _settingsData = new SettingsData();
            SukiHost.ShowToast("Error!", "Database doesn't exist - go to Settings");
            SettingsDialogViewModel newDialog = new SettingsDialogViewModel();
            SukiHost.ShowDialog(newDialog);
        }
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

    public static SettingsData GetSettings()
    {
        return _settingsData != null ? _settingsData : _dbContext.SettingsData.Single();
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

    public static YTMusicHelper GetYTMusicHelper()
    {
        return _ytmHelper;
    }

    public static DefaultViewModel GetVM()
    {
        return _mainViewModel;
    }
}