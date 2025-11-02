using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels;

public partial class ArtistListViewModel : ObservableValidator
{
    private ObservableCollection<ArtistViewModel> _artistCards;
    [ObservableProperty] private string _busyText;
    [ObservableProperty] private bool _isBusy;

    public ArtistListViewModel()
    {
        if (App.GetDBContext().Exists()) LoadArtists();
    }

    public ObservableCollection<ArtistViewModel> ArtistCards
    {
        get => _artistCards;
        set
        {
            _artistCards = value;
            OnPropertyChanged();
        }
    }

    public void AddArtist()
    {
        /*
        var newVM = new NewArtistDialogViewModel();
        newVM.ClosePageEvent += RefreshArtists;
        */
        var newVm = NewResultDialogViewModel.CreateResultSearch(typeof(ArtistResult));
        //var newVM = new NewArtistDialogViewModel();
        newVm.ClosePageEvent += RefreshArtists;
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => newVm)
            .TryShow();
    }

    public async void InitData()
    {
        BusyText = "Building Database...";
        IsBusy = true;
        App.GetDBHelper().ProgressUpdate += UpdateInitProgressText;
        var textTest = false;
        LoadArtists();
    }

    public async void LoadArtists()
    {
        BusyText = "Loading Artists...";
        IsBusy = true;
        if (ArtistCards != null) ArtistCards.Clear();
        ArtistCards = await App.GetDBHelper().GenerateArtists();
        IsBusy = false;
    }

    public void AddArtistToList(Artist newArtist)
    {
        ArtistCards.Add(new ArtistViewModel(newArtist));
    }

    private void RefreshArtists(object? sender, bool t)
    {
        LoadArtists();
    }

    public void UpdateInitProgressText(int progress)
    {
        BusyText = $"Building Database...\n\t\t  {progress}/100";
    }

    [RelayCommand]
    private async Task ToggleBusy()
    {
        BusyText = "Testing Busy Window..";
        IsBusy = true;
        await Task.Delay(3000);
        IsBusy = false;
    }
}