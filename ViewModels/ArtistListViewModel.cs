using MVNFOEditor.DB;
using MVNFOEditor.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Material.Icons;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Views;
using ReactiveUI;
using SimpleInjector.Advanced;
using SukiUI.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistListViewModel : ObservableValidator
    {
        private MusicDBHelper DBHelper;
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
            DBHelper = App.GetDBHelper();
            if (DBHelper.CheckIfSettingsValid())
            {
                LoadArtists();
            }
            
        }

        public void AddArtist()
        {
            NewArtistDialogViewModel newVM = new NewArtistDialogViewModel();
            newVM.ClosePageEvent += RefreshArtists;
            SukiHost.ShowDialog(newVM);
        }

        public async void InitData()
        {
            BusyText = "Building Database...";
            IsBusy = true;
            DBHelper.ProgressUpdate += UpdateInitProgressText;
            await DBHelper.InitilizeData();
            bool textTest = false;
            await LoadArtists();
        }

        public async Task<bool> LoadArtists()
        {
            BusyText = "Loading Artists...";
            IsBusy = true;
            if (ArtistCards != null)
            {
                ArtistCards.Clear();
            }
            ArtistCards = await DBHelper.GenerateArtists();
            IsBusy = false;
            return true;
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