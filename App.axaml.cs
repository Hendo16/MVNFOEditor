using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.ViewModels;
using MVNFOEditor.Views;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System.ComponentModel;

namespace MVNFOEditor;

public partial class App : Application
{
    private static MusicDbContext _dbContext;
    private static YTDLHelper _ytdlHelper;
    private static YTMusicHelper _ytmHelper;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _dbContext = new MusicDbContext();
        _dbContext.Database.EnsureCreated();
        _ytdlHelper = new YTDLHelper();
        _ytmHelper = new YTMusicHelper();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        base.OnFrameworkInitializationCompleted();


        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static MusicDbContext GetDBContext()
    {
        return _dbContext;
    }

    public static YTDLHelper GetYTDLHelper()
    {
        return _ytdlHelper;
    }

    public static YTMusicHelper GetYTMusicHelper()
    {
        return _ytmHelper;
    }
}
