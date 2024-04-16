using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using MVNFOEditor.Helpers;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using YoutubeDLSharp;
using MVNFOEditor.DB;
using SukiUI.Controls;
using YoutubeDLSharp.Metadata;
using Size = Avalonia.Size;

namespace MVNFOEditor.ViewModels
{
    public partial class AddMusicVideoParentViewModel : ObservableObject
    {
        private ManualMusicVideoViewModel? _manualVM;
        private SyncDialogViewModel? _resultsVM;
        private YTDLHelper _ytDLHelper;
        private MusicDbContext _dbContext;
        private SettingsData localData;
        public event EventHandler<bool> RefreshAlbumEvent;

        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private bool _navVisible;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _showError;
        [ObservableProperty] private string _errorText;
        private bool _edit;

        public AddMusicVideoParentViewModel(ManualMusicVideoViewModel vm, bool edit = false)
        {
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            localData = _dbContext.SettingsData.First();
            _manualVM = vm;
            CurrentContent = _manualVM;
            _edit = edit;
            NavVisible = true;
        }

        public AddMusicVideoParentViewModel(SyncDialogViewModel vm)
        {
            _resultsVM = vm;
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            localData = _dbContext.SettingsData.First();
            CurrentContent = _resultsVM;
            NavVisible = true;
        }

        public async void BuildProgressVM(object? sender, SyncResultViewModel? _resultVM)
        {
            NavVisible = false;
            SyncResult _result = _resultVM.GetResult();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _result.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_result.vidID, $"{localData.RootFolder}/{_result.Artist.Name}", _result.Title, progress);
            if (downloadResult.Success)
            {
                await _resultVM.GenerateNFO(downloadResult.Data);
                RefreshAlbumEvent?.Invoke(sender, true);
                CurrentContent = _resultsVM;
                NavVisible = true;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.CloseDialog();
                SukiHost.ShowToast("Download Error", errorContent[0]);
            }
        }

        public async void HandleSave()
        {
            NavVisible = false;
            if (_manualVM == null)
            {
                await SaveMultipleVideos();
                CurrentContent = _resultsVM;
            }
            else if (_resultsVM == null)
            {
                if (_manualVM.YTVisible)
                {
                    SaveVideo();
                }
                else
                {
                    SaveManualVideo();
                }
            }
        }

        public async Task<bool> SaveMultipleVideos()
        {
            SettingsData localData = _dbContext.SettingsData.First();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            bool result = true;
            CurrentContent = waveVM;
            for (int i = 0; i < _resultsVM.SelectedVideos.Count; i++)
            {
                if (_resultsVM.SelectedVideos.Count > 1)
                {
                    waveVM.HeaderText = $"Downloading {_resultsVM.SelectedVideos[i].Title} {i + 1}/{_resultsVM.SelectedVideos.Count}";
                }
                else
                {
                    waveVM.HeaderText = $"Downloading {_resultsVM.SelectedVideos[i].Title}";
                }
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
                var currResult = _resultsVM.SelectedVideos[i].HandleDownload();
                var downResult = await _ytDLHelper.DownloadVideo(currResult.vidID, $"{localData.RootFolder}/{currResult.Artist.Name}", currResult.Title, progress);
                if (downResult.Success)
                {
                    _resultsVM.SelectedVideos[i].GenerateNFO(downResult.Data).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            SukiHost.ShowToast("Error", $"Something went wrong with downloading {_resultsVM.SelectedVideos[i].Title}");
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    SukiHost.ShowToast("Error", $"Something went wrong with downloading {_resultsVM.SelectedVideos[i].Title}");
                    break;
                }
            }
            return result;
        }

        public async void SaveVideo()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualVM._vidData.ID, $"{localData.RootFolder}/{_manualVM.Artist.Name}", _manualVM.Title, progress);
            if (downloadResult.Success)
            {
                GenerateManualNFO(downloadResult.Data, false);
                RefreshAlbumEvent?.Invoke(null, true);
                _manualVM.ClearData();
                CurrentContent = _manualVM;
                NavVisible = true;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.ShowToast("Download Error", errorContent[0]);
                NavVisible = true;
            }
        }

        public void SaveManualVideo()
        {
            SettingsData localData = App.GetDBContext().SettingsData.First();
            string newPath;
            //If the local file already matches the 'expected file naming structure', don't rename it further
            if (Path.GetFileName(_manualVM.VideoPath) !=
                $"{_manualVM.Title}-video{Path.GetExtension(_manualVM.VideoPath)}")
            { newPath = $"{localData.RootFolder}\\{_manualVM.Artist.Name}\\{_manualVM.Title}-video{Path.GetExtension(_manualVM.VideoPath)}"; }
            else { newPath = $"{localData.RootFolder}\\{_manualVM.Artist.Name}\\{Path.GetFileName(_manualVM.VideoPath)}"; }
            //If the artist folder doesn't exist yet, create it
            if (!Directory.Exists($"{localData.RootFolder}\\{_manualVM.Artist.Name}"))
            {
                Directory.CreateDirectory($"{localData.RootFolder}\\{_manualVM.Artist.Name}");
            }
            if (_edit)
            {
                //Delete the original video and thumbnail
                File.Delete(_manualVM.PreviousVideo.vidPath);
                File.Delete($"{localData.RootFolder}\\{_manualVM.Artist.Name}\\{_manualVM.PreviousVideo.thumb}");
                //Move the new one over
                File.Move(_manualVM.VideoPath, newPath);
                //Re-generate the NFO
                GenerateManualNFO(newPath, true);
            }
            else
            {
                //Move the local file over
                File.Move(_manualVM.VideoPath, newPath);
                //Generate a new NFO
                GenerateManualNFO(newPath, false);
            }
            NavVisible = true;
        }

        public void Close()
        {
            _resultsVM = null;
            _manualVM = null;
            SukiHost.CloseDialog();
        }
        
        private async void GenerateManualNFO(string vidPath, bool edit)
        {
            if (edit)
            {
                _dbContext.MusicVideos.Remove(_manualVM.PreviousVideo);
            }
            MusicVideo newMV = new MusicVideo();
            newMV.title = _manualVM.Title;
            newMV.year = _manualVM.Year;
            newMV.artist = _manualVM.Artist;
            newMV.vidPath = vidPath;
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
                await _manualVM.SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}", newMV.title);
                newMV.thumb = $"{newMV.title}-video.jpg";
            }
            else
            {
                newMV.source = "local";
                var newImagePath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.png";
                FFMpeg.Snapshot(vidPath, newImagePath, new System.Drawing.Size(400, 225), TimeSpan.FromSeconds(localData.ScreenshotSecond));
                newMV.thumb = $"{newMV.title}-video.png";
            }

            newMV.nfoPath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";
            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
            RefreshAlbumEvent?.Invoke(null, true);
            Close();
        }
    }
}