using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class MusicVideoDetailsViewModel : ObservableObject
{
    private readonly ArtistListParentViewModel _parentVM;
    private readonly MusicDBHelper DBHelper;
    private MusicVideo _musicVideo;
    private Bitmap? _thumbnail;
    [ObservableProperty] private Album _currAlbum;
    [ObservableProperty] private List<Album> _albums;
    [ObservableProperty] private string _aspectRatio;
    [ObservableProperty] private string _bitrate;
    [ObservableProperty] private string _codec;
    [ObservableProperty] private string _duration;
    [ObservableProperty] private string _resolution;
    [ObservableProperty] private NFODetails _nfoDetails = new();
    
    public MusicVideoDetailsViewModel()
    {
        DBHelper = App.GetDBHelper();
        _parentVM = App.GetVM().GetParentView();
        var settingsData = App.GetSettings();
        GlobalFFOptions.Configure(new FFOptions
        {
            BinaryFolder = Path.GetFullPath(settingsData.FFPROBEPath),
            TemporaryFilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "FFPROBE", "tmp")
        });
        _nfoDetails = new NFODetails();
    }

    public static async Task<MusicVideoDetailsViewModel> CreateViewModel(MusicVideo video)
    {
        var newVM = new MusicVideoDetailsViewModel();
        newVM.SetVideo(video);
        await newVM.AnalyzeVideo();
        await newVM.LoadThumbnail();
        return newVM;
    }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public async void UpdateMusicVideo()
    {
        var originalTitle = _musicVideo.title;

        _musicVideo.title = NfoDetails.Title;
        _musicVideo.year = NfoDetails.Year.ToString();
        _musicVideo.source = NfoDetails.Source;
        _musicVideo.album = CurrAlbum;
        //TODO: Handle better blank album entry
        //if (_currAlbum.Title == "") {_musicVideo.album = new Album();} else{ _musicVideo.album = _currAlbum; }

        //Handle title changes
        if (originalTitle != NfoDetails.Title)
        {
            var nfoPath = _musicVideo.nfoPath;
            var folderPath = Path.GetDirectoryName(nfoPath);
            var thumbFileName = folderPath + "/" + _musicVideo.thumb;
            if (File.Exists(thumbFileName))
            {
                File.Move(thumbFileName, folderPath + $"/{NfoDetails.Title}.jpg");
                _musicVideo.thumb = $"{NfoDetails.Title}.jpg";
            }

            if (File.Exists(nfoPath))
            {
                File.Move(nfoPath, folderPath + $"/{NfoDetails.Title}.nfo");
                _musicVideo.nfoPath = folderPath + $"\\{NfoDetails.Title}.nfo";
            }

            //Get Video
            var videoPath = _musicVideo.vidPath;
            if (videoPath.Length > 0)
            {
                var ext = Path.GetExtension(videoPath);
                File.Move(videoPath, folderPath + $"/{NfoDetails.Title}{ext}");
                _musicVideo.vidPath = folderPath + $"\\{NfoDetails.Title}{ext}";
            }
        }

        var success = await DBHelper.UpdateMusicVideo(_musicVideo);
        if (success == 0) Debug.WriteLine(success);
        NavigateBack();
    }

    public void SetVideo(MusicVideo video)
    {
        _musicVideo = video;
        NfoDetails.Title = _musicVideo.title;
        NfoDetails.Year = int.Parse(_musicVideo.year);
        NfoDetails.Source = _musicVideo.source;
        Albums = DBHelper.GetAlbums(video.artist.Id);
        if (video.album != null) CurrAlbum = Albums.Find(a => a.Id == video.album.Id);
    }
    
    public async Task AnalyzeVideo()
    {
        var info = await FFProbe.AnalyseAsync(_musicVideo.vidPath);
        var minutes = (int)info.Duration.TotalMinutes;
        var seconds = (int)((info.Duration.TotalMinutes -
                             (int)info.Duration.TotalMinutes) * 60);
        Codec = $"Codec: {info.PrimaryVideoStream.CodecName}";
        Duration = minutes != 0 ? $"Duration: {minutes:D2}:{seconds:D2}" : "Duration: unavailable";
        Resolution = $"Resolution: {info.PrimaryVideoStream.Width}" + "x" + $"{info.PrimaryVideoStream.Height}";
        //Bitrate = $"AverageBitrate: {info.Format.BitRate} kb/s";
        
        Bitrate = info.Format.BitRate / 1000000.0 >= 1
            ? $"AverageBitrate: {Math.Round(info.Format.BitRate / 1000000.0)} Mbps"
            : $"AverageBitrate: {Math.Round(info.Format.BitRate / 1000.0)} kbps";
        
        AspectRatio = info.PrimaryVideoStream.DisplayAspectRatio.Width != 0
            ? $"Aspect Ratio: {info.PrimaryVideoStream.DisplayAspectRatio.Width}:{info.PrimaryVideoStream.DisplayAspectRatio.Height}"
            : "Aspect Ratio unavailable";
    }

    public async Task LoadThumbnail()
    {
        await using (var imageStream = await _musicVideo.LoadThumbnailBitmapAsync())
        {
            if (imageStream != null)
                try
                {
                    Thumbnail = new Bitmap(imageStream);
                }
                catch (ArgumentException e)
                {
                }
        }
    }

    public void NavigateBack()
    {
        _parentVM.BackToDetails(true);
    }

    public void DeleteVideo()
    {
        if (DBHelper.DeleteVideo(_musicVideo) != 1)
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error Deleting Video")
                .WithContent($"Please manually delete the video for {_musicVideo.title}")
                .OfType(NotificationType.Error)
                .Queue();
        NavigateBack();
    }
}