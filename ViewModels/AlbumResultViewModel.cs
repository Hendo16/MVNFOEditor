using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class AlbumResultViewModel : ObservableObject
    {
        private readonly AlbumResult _result;
        private Bitmap? _thumbnail;
        private string _borderColor;
        private string _selectBtnText;
        private MusicDbContext _dbContext;
        
        public string Title => _result.Title;
        public string Year => _result.Year;
        public bool? IsExplicit => _result.isExplicit;

        public string SelectBtnText
        {
            get { return _selectBtnText; }
            set
            {
                _selectBtnText = value;
                OnPropertyChanged(nameof(SelectBtnText));
            }
        }

        public event EventHandler<AlbumResult> NextPage;

        public string BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor));
            }
        }
        
        public Bitmap? Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public AlbumResultViewModel(AlbumResult result)
        {
            //_dbContext = App.GetDBContext();
            _result = result;
            SelectBtnText = "Select";
            //SelectBtnText = _dbContext.Album.Any(a => a.AlbumBrowseID == result.browseId) ? "Open" : "Select";
        }

        public static async Task<ObservableCollection<AlbumResultViewModel>> GetAlbumResultVM(List<AlbumResult> results)
        {
            ObservableCollection<AlbumResultViewModel>
                returnedModels = new ObservableCollection<AlbumResultViewModel>();
            for (int i = 0; i < results.Count; i++)
            {
                AlbumResultViewModel newVM = new AlbumResultViewModel(results[i]);
                await newVM.LoadThumbnail();
                returnedModels.Add(newVM);
            }
            return returnedModels;
        }

        public AlbumResult GetResult()
        {
            return _result;
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
