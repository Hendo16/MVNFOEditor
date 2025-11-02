using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public class SingleViewModel : ObservableObject
{
    private readonly MusicVideo _video;

    private Bitmap? _thumbnail;

    public SingleViewModel(MusicVideo video)
    {
        _video = video;
    }

    public string Title => _video.title;
    public string Year => _video.year;

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadThumbnail()
    {
        await using (var imageStream = await _video.LoadThumbnailBitmapAsync())
        {
            if (imageStream != null)
            {
                try
                {
                    Thumbnail = Bitmap.DecodeToWidth(imageStream, 240);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Couldn't find {_video.thumb}");
                }
            }
        }
    }

    public async void EditVideo()
    {
        if (File.Exists(_video.vidPath))
        {
            var vidDetailsVm = new MusicVideoDetailsViewModel();
            vidDetailsVm.SetVideo(_video);
            vidDetailsVm.AnalyzeVideo();
            await vidDetailsVm.LoadThumbnail();
            App.GetVM().GetParentView().CurrentContent = vidDetailsVm;
        }
        else
        {
            //Handle deleted video
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error!")
                .WithContent($"{_video.title} has been deleted - removing from db...")
                .OfType(NotificationType.Error)
                .Queue();
            App.GetDBContext().MusicVideos.Remove(_video);
            App.GetDBContext().SaveChanges();
        }
    }
}