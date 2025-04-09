using Avalonia.Media.Imaging;
using MVNFOEditor.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public class SingleViewModel : ObservableObject
    {
        private readonly MusicVideo _video;
        public string Title => _video.title;
        public string Year => _video.year;
        public Artist Artist => _video.artist;

        private Bitmap? _thumbnail;

        public Bitmap? Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public SingleViewModel(MusicVideo video)
        {
            _video = video;
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
                    };
                }
            }
        }

        public async void EditVideo()
        {
            if (File.Exists(_video.vidPath))
            {
                MusicVideoDetailsViewModel vidDetailsVM = new MusicVideoDetailsViewModel();
                vidDetailsVM.SetVideo(_video);
                vidDetailsVM.AnalyzeVideo();
                await vidDetailsVM.LoadThumbnail();
                App.GetVM().GetParentView().CurrentContent = vidDetailsVM;
            }
            else{
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
}