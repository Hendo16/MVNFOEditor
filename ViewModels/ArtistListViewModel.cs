using MVNFOEditor.DB;
using MVNFOEditor.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Material.Icons;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Views;
using SimpleInjector.Advanced;
using SukiUI.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistListViewModel : ObservableValidator
    {
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        private ObservableCollection<ArtistViewModel> _artistCards;
        public ObservableCollection<ArtistViewModel> ArtistCards
        {
            get { return _artistCards; }
            set
            {
                _artistCards = value;
                OnPropertyChanged(nameof(ArtistCards));
            }
        }

        public ArtistListViewModel()
        {
            if (App.GetDBContext().Exists())
            {
                LoadArtists();
            }
        }

        public void AddArtist()
        {
            NewArtistDialogViewModel newVM = new NewArtistDialogViewModel();
            newVM.ClosePageEvent += RefreshArtists;
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => newVM)
                .TryShow();
        }

        public async void InitData()
        {
            BusyText = "Building Database...";
            IsBusy = true;
            App.GetDBHelper().ProgressUpdate += UpdateInitProgressText;
            bool textTest = false; 
            LoadArtists();
        }

        public async void LoadArtists()
        {
            BusyText = "Loading Artists...";
            IsBusy = true;
            if (ArtistCards != null)
            {
                ArtistCards.Clear();
            }
            ArtistCards = await App.GetDBHelper().GenerateArtists();
            IsBusy = false;
        }
        public void RefreshArtists(object? sender, bool t)
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
}