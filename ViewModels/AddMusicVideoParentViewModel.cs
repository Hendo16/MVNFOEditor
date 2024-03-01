using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using MVNFOEditor.Helpers;
using System;
using System.Linq;
using YoutubeDLSharp;
using MVNFOEditor.DB;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public class AddMusicVideoParentViewModel : ObservableObject
    {
        private ManualMusicVideoViewModel _manualVM;
        private SyncDialogViewModel _resultsVM;
        private YTDLHelper _ytDLHelper;
        private MusicDbContext _dbContext;
        private SettingsData localData;
        private object _currentContent;
        public event EventHandler<bool> RefreshAlbumEvent;
        public object CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
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

        public AddMusicVideoParentViewModel(ManualMusicVideoViewModel vm)
        {
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            localData = _dbContext.SettingsData.First();
            _manualVM = vm;
            CurrentContent = _manualVM;
            NextEnabled = true;
        }

        public AddMusicVideoParentViewModel(SyncDialogViewModel vm)
        {
            _resultsVM = vm;
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            localData = _dbContext.SettingsData.First();
            CurrentContent = _resultsVM;
            NextEnabled = false;
        }

        public async void BuildProgressVM(object? sender, SyncResult? _result)
        {
            SettingsData localData = _dbContext.SettingsData.First();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _result.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_result.vidID, $"{localData.RootFolder}/{_result.Artist.Name}", _result.Title, progress);
            if (downloadResult.Success)
            {
                RefreshAlbumEvent?.Invoke(sender, true);
                CurrentContent = _resultsVM;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.ShowToast("Download Error", errorContent[0]);
            }
        }

        public async void SaveVideo()
        {
            GenerateManualNFO();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualVM._vidData.ID, $"{localData.RootFolder}/{_manualVM.Artist.Name}", _manualVM.Title, progress);
            if (downloadResult.Success)
            {
                _manualVM.ClearData();
                CurrentContent = _manualVM;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.ShowToast("Download Error", errorContent[0]);
            }
        }

        public void HandleChangedMode(bool? changeValue)
        {
            if ((bool)!changeValue)
            {
                //ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel();
                //CurrentContent = newVM;
                //_manualVM = newVM;
                NextEnabled = true;
            }
            else
            {
                //CurrentContent = _resultsVM;
                NextEnabled = false;
            }
        }

        public void Close()
        {
            SukiHost.CloseDialog();
        }


        private async void GenerateManualNFO()
        {
            MusicVideo newMV = new MusicVideo();
            newMV.title = _manualVM.Title;
            newMV.year = _manualVM.Year;
            newMV.artist = _manualVM.Artist;
            if (_manualVM.CurrAlbum != null)
            {
                newMV.album = _manualVM.CurrAlbum;
            }
            else
            {
                newMV.album = null;
            }

            if (_manualVM._vidData != null)
            {
                newMV.videoID = _manualVM._vidData.ID;
                newMV.source = "youtube";
            }
            else
            {
                newMV.source = "local";
            }
            newMV.filePath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";

            await _manualVM.SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}");
            newMV.thumb = $"{newMV.title}-video.jpg";

            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
        }
    }
}