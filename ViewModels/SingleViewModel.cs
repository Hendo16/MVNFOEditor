using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class SingleViewModel : ReactiveObject
    {
        private readonly MusicVideo _video;
        private YTMusicHelper ytMusicHelper;
        public string Title => _video.title;
        public string Year => _video.year;
        public Artist Artist => _video.artist;

        private Bitmap? _thumbnail;

        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        public SingleViewModel(MusicVideo video)
        {
            _video = video;
            ytMusicHelper = App.GetYTMusicHelper();
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
            MusicVideoDetailsViewModel vidDetailsVM = new MusicVideoDetailsViewModel();
            vidDetailsVM.SetVideo(_video);
            vidDetailsVM.AnalyzeVideo();
            await vidDetailsVM.LoadThumbnail();
            App.GetVM().GetParentView().CurrentContent = vidDetailsVM;
        }
    }
}