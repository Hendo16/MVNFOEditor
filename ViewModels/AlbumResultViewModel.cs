using Avalonia.Media.Imaging;
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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public class AlbumResultViewModel : ReactiveObject
    {
        private readonly AlbumResult _result;
        private Bitmap? _thumbnail;
        private string _borderColor;
        private string _selectBtnText;
        private MusicDbContext _dbContext;
        private YTMusicHelper _musicHelper;
        private YTDLHelper _ytDLHelper;
        
        public string Title => _result.Title;
        public string Year => _result.Year;
        public string BrowseId => _result.browseId;
        public bool? IsExplicit => _result.isExplicit;

        public string SelectBtnText
        {
            get => _selectBtnText;
            set => this.RaiseAndSetIfChanged(ref _selectBtnText, value);
        }

        public event EventHandler<AlbumResult> NextPage;

        public string BorderColor
        {
            get => _borderColor;
            set => this.RaiseAndSetIfChanged(ref _borderColor, value);
        }

        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        public AlbumResultViewModel(AlbumResult result)
        {
            _dbContext = App.GetDBContext();
            _musicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _result = result;
            SelectBtnText = _dbContext.Album.Any(a => a.ytMusicBrowseID == result.browseId) ? "Open" : "Select";
        }

        public void GrabAlbum()
        {
            SelectBtnText = "Open";
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
