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

public partial class NFODetails : ObservableObject
{
    [ObservableProperty] [property: DisplayName("Genre Test")]
    private List<SearchSource> _genres;

    [ObservableProperty] [property: DisplayName("Source")]
    private SearchSource _source;

    [ObservableProperty] [property: DisplayName("Title")]
    private string? _title;

    [ObservableProperty] [property: DisplayName("Year")]
    private int? _year;
}

public partial class MusicVideoDetailsViewModel : ObservableObject
{
    private readonly ArtistListParentViewModel _parentVM;
    private readonly MusicDBHelper DBHelper;
    [ObservableProperty] private List<Album> _albums;
    [ObservableProperty] private string _aspectRatio;
    [ObservableProperty] private string _bitrate;

    [ObservableProperty] private string _codec;
    private Album _currAlbum;
    [ObservableProperty] private string _duration;
    private MusicVideo _musicVideo;
    [ObservableProperty] private NFODetails _nfoDetails;
    [ObservableProperty] private string _resolution;
    [ObservableProperty] private string _source;
    private Bitmap? _thumbnail;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _year;

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

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public Album CurrAlbum
    {
        set
        {
            _currAlbum = value;
            OnPropertyChanged();
        }
    }

    public async void UpdateMusicVideo()
    {
        var originalTitle = _musicVideo.title;

        _musicVideo.title = Title;
        _musicVideo.year = Year;
        //TODO: Handle better blank album entry
        //if (_currAlbum.Title == "") {_musicVideo.album = new Album();} else{ _musicVideo.album = _currAlbum; }

        //Handle title changes
        if (originalTitle != Title)
        {
            var nfoPath = _musicVideo.nfoPath;
            var folderPath = Path.GetDirectoryName(nfoPath);
            var thumbFileName = folderPath + "/" + _musicVideo.thumb;
            if (File.Exists(thumbFileName))
            {
                File.Move(thumbFileName, folderPath + $"/{Title}.jpg");
                _musicVideo.thumb = $"{Title}.jpg";
            }

            if (File.Exists(nfoPath))
            {
                File.Move(nfoPath, folderPath + $"/{Title}.nfo");
                _musicVideo.nfoPath = folderPath + $"\\{Title}.nfo";
            }

            //Get Video
            var videoPath = _musicVideo.vidPath;
            if (videoPath.Length > 0)
            {
                var ext = Path.GetExtension(videoPath);
                File.Move(videoPath, folderPath + $"/{Title}{ext}");
                _musicVideo.vidPath = folderPath + $"\\{Title}{ext}";
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
        Albums = DBHelper.GetAlbums(video.artist.Id);
        if (Enum.TryParse<SearchSource>(_musicVideo.source, out var source)) NfoDetails.Source = source;
        if (video.album != null) CurrAlbum = Albums.Find(a => a.Id == video.album.Id);
    }
    
    public async void AnalyzeVideo()
    {
        var info = await FFProbe.AnalyseAsync(_musicVideo.vidPath);
        var minutes = (int)info.PrimaryVideoStream.Duration.TotalMinutes;
        var seconds = (int)((info.PrimaryVideoStream.Duration.TotalMinutes -
                             (int)info.PrimaryVideoStream.Duration.TotalMinutes) * 60);
        Codec = $"Codec: {info.PrimaryVideoStream.CodecName}";
        Duration = minutes != 0 ? $"Duration: {minutes:D2}:{seconds:D2}" : "Duration: unavaible";
        Resolution = $"Resolution: {info.PrimaryVideoStream.Width}" + "x" + $"{info.PrimaryVideoStream.Height}";
        Bitrate = info.PrimaryVideoStream.BitRate / 1000000.0 >= 1
            ? $"AverageBitrate: {Math.Round(info.PrimaryVideoStream.BitRate / 1000000.0)} Mbps"
            : $"AverageBitrate: {Math.Round(info.PrimaryVideoStream.BitRate / 1000.0)} kbps";
        AspectRatio = info.PrimaryVideoStream.DisplayAspectRatio.Width != 0
            ? $"Aspect Ratio: {info.PrimaryVideoStream.DisplayAspectRatio.Width}:{info.PrimaryVideoStream.DisplayAspectRatio.Height}"
            : "Aspect Ratio unavaliable";
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