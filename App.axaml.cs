using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.ViewModels;
using MVNFOEditor.Views;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System.ComponentModel;

namespace MVNFOEditor;

public partial class App : Application
{
    private MusicDbContext _dbContext;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _dbContext = new MusicDbContext();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_dbContext)
            };
            
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel(_dbContext)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
