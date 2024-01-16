using System;
using System.Collections.Generic;
using MVNFOEditor.DB;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public event EventHandler<string> RootFolderChanged;
    private string _rootFolder;
    public MusicDbContext MVDBContext { get; set; }

    public string RootFolder
    {
        get => _rootFolder;
        set
        {
            if (_rootFolder != value)
            {
                _rootFolder = value;
                // Notify the change to the MainViewModel
                // You can use an event or a messaging system to notify the change
                // e.g., EventAggregator.Publish(new RootFolderChangedMessage(value));
                OnPropertyChanged();
                OnRootFolderChanged(value);
            }
        }
    }
    
    public MainViewModel(MusicDbContext dbContext)
    {
        MVDBContext = dbContext;
    }

    protected virtual void OnRootFolderChanged(string folder)
    {
        RootFolderChanged?.Invoke(this, folder);
    }
}