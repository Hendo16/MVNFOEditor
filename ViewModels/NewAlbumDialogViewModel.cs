using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.Views;
using MVNFOEditor.Helpers;
using SukiUI.Controls;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Newtonsoft.Json.Linq;
using MVNFOEditor.DB;

namespace MVNFOEditor.ViewModels
{
    public partial class NewAlbumDialogViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        private AlbumResultsViewModel _resultVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMVVM;
        private AddMusicVideoParentViewModel? _syncVM;
        private YTMusicHelper ytMusicHelper;
        private MusicDbContext _dbContext;
        private Artist _artist;
        [ObservableProperty] private bool _isBusy;

        public event EventHandler<bool> ClosePageEvent;

        public IEnumerable<string> Steps { get; set; } = [
        "Select Album",
        "Select Videos"
        ];
        private bool _toggleEnable;

        public bool ToggleEnable
        {
            get { return _toggleEnable; }
            set
            {
                _toggleEnable = value;
                OnPropertyChanged(nameof(ToggleEnable));
            }
        }
        private bool _toggleValue;

        public bool ToggleValue
        {
            get { return _toggleValue; }
            set
            {
                _toggleValue = value;
                OnPropertyChanged(nameof(ToggleValue));
            }
        }
        private bool _nextEnabled;

        public bool NextEnabled
        {
            get { return _nextEnabled; }
            set
            {
                _nextEnabled = value;
                OnPropertyChanged(nameof(NextEnabled));
            }
        }

        private string _backButtonText;
        public string BackButtonText
        {
            get { return _backButtonText; }
            set
            {
                _backButtonText = value;
                OnPropertyChanged(nameof(BackButtonText));
            }
        }

        private object _currentContent;
        public object CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        private int _stepIndex = 0;
        public int StepIndex
        {
            get { return _stepIndex; }
            set
            {
                _stepIndex = value;
                OnPropertyChanged(nameof(StepIndex));
            }
        }

        public NewAlbumDialogViewModel(AlbumResultsViewModel vm, Artist artist)
        {
            _resultVM = vm;
            CurrentContent = vm;
            _syncVM = null;
            ytMusicHelper = App.GetYTMusicHelper();
            _dbContext = App.GetDBContext();
            ToggleEnable = true;
            ToggleValue = true;
            BackButtonText = "Exit";
            NextEnabled = false;
            _parentVM = App.GetVM().GetParentView();
            _artist = artist;
        }

        public async void NextStep(object? sender, AlbumResult newAlbum)
        {
            Album album = null;
            if (!_dbContext.Album.Any(a => a.ytMusicBrowseID == newAlbum.browseId))
            {
                album = new Album(newAlbum);
                _dbContext.Album.Add(album);
                _dbContext.SaveChanges();
                _parentVM.RefreshDetails();
            }
            else
            {
                album = _dbContext.Album.First(a => a.ytMusicBrowseID == newAlbum.browseId);
            }

            string artistID = album.Artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            JObject fullAlbumDetails = ytMusicHelper.get_album(album.ytMusicBrowseID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, fullAlbumDetails, null, album);

            if (results.Count == 0)
            {
                TextBlock errBox = new TextBlock() { Text = "No Videos Available" };
                CurrentContent = errBox;
            }
            else
            {
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                _syncVM = parentVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }
                HandleNavigation(true);
            }
        }

        [RelayCommand]
        public async void HandleNavigation(bool isIncrement)
        {
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex >= Steps.Count() - 1:
                case false when StepIndex <= 0:
                    SukiHost.CloseDialog();
                    return;
                default:
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    break;
            }

            if (currentType == typeof(ManualAlbumViewModel))
            {
                IsBusy = true;
                ToggleEnable = false;
                var album = await _manualAlbumVM.SaveAlbum();
                ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(album);
                _manualMVVM = newVM;

                Steps = ["Create Album", "Create Video"];

                CurrentContent = newVM;
                IsBusy = false;
            }
            
            if (currentType == typeof(AlbumResultsViewModel))
            {
                if (isIncrement)
                {
                    CurrentContent = _syncVM;
                }
                else
                {
                    CurrentContent = _resultVM;
                }
            }

            if (currentType == typeof(AddMusicVideoParentViewModel))
            {
                if (!isIncrement)
                {
                    CurrentContent = _resultVM;
                }
            }
        }

        public void HandleChangedMode(bool? changeValue)
        {
            if ((bool)!changeValue)
            {
                ManualAlbumViewModel newVM = new ManualAlbumViewModel(_artist);
                CurrentContent = newVM;
                _manualAlbumVM = newVM;
                NextEnabled = true;
            }
            else
            {
                CurrentContent = _resultVM;
            }
        }

        public void BackTrigger() { HandleNavigation(false); }
        public void NextTrigger() { HandleNavigation(true); }
        [RelayCommand]
        public void CloseDialog()
        {
            ClosePageEvent?.Invoke(this,true);
            SukiHost.CloseDialog();
        }
    }
}
