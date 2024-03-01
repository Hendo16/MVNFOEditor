using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class ArtistResultViewModel : ReactiveObject
    {
        private readonly ArtistResult _result;
        private Bitmap? _thumbnail;
        private string _borderColor;
        private bool _loading;
        private string _selectText;
        private MusicDbContext _dbContext;
        
        public string Name => _result.Name;
        public string BrowseId => _result.browseId;

        public bool Loading
        {
            get => _loading;
            set => this.RaiseAndSetIfChanged(ref _loading, value);
        }

        public string SelectButtonText
        {
            get => _selectText;
            set => this.RaiseAndSetIfChanged(ref _selectText, value);
        }

        public event EventHandler<ArtistResult> NextPage;

        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        public ArtistResultViewModel(ArtistResult result)
        {
            _dbContext = App.GetDBContext();
            Loading = false;
            SelectButtonText = "Select";
            _result = result;
        }

        public void GrabArtist()
        {
            Loading = true;
            SelectButtonText = "Added";
            TriggerNextPage();
        }

        public async Task LoadThumbnail()
        {
            await using (var imageStream = await _result.LoadCoverBitmapAsync())
            {
                Thumbnail = new Bitmap(imageStream);
            }
        }

        public async Task SaveThumbnailAsync(string folderPath)
        {
            var bitmap = Thumbnail;
            await Task.Run(() =>
            {
                using (var fs = _result.SaveThumbnailBitmapStream(folderPath))
                {
                    bitmap.Save(fs);
                }
            });
        }

        protected virtual void TriggerNextPage()
        {
            NextPage?.Invoke(this, _result);
        }
    }
}