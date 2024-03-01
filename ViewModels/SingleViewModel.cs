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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class SingleViewModel : ReactiveObject
    {
        private ArtistListParentViewModel _parentVM;
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
            _parentVM = App.GetVM().GetParentView();
            ytMusicHelper = App.GetYTMusicHelper();
        }

        public async Task LoadThumbnail()
        {
            await using (var imageStream = await _video.LoadThumbnailBitmapAsync())
            {
                if (imageStream != null)
                {
                    try {Thumbnail = new Bitmap(imageStream);} catch (ArgumentException e){}
                }
            }
        }

        public void EditVideo()
        {
            MusicVideoDetailsViewModel vidDetailsVM = new MusicVideoDetailsViewModel();
            vidDetailsVM.SetVideo(_video);
            _parentVM.CurrentContent = vidDetailsVM;
        }
    }
}