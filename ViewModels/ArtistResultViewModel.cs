using Avalonia.Media.Imaging;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVNFOEditor.ViewModels
{
    public class ArtistResultViewModel : ObservableObject
    {
        private readonly ArtistResult _result;
        private Bitmap? _thumbnail;
        private string _borderColor;
        private bool _loading;
        private string _selectText;
        
        public string Name => _result.Name;

        public bool Loading
        {
            get { return _loading; }
            set
            {
                _loading = value;
                OnPropertyChanged(nameof(Loading));
            }
        }

        public string SelectButtonText
        {
            get { return _selectText; }
            set
            {
                _selectText = value;
                OnPropertyChanged(nameof(SelectButtonText));
            }
        }

        public event EventHandler<ArtistResult> NextPage;

        public Bitmap? Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public ArtistResultViewModel(ArtistResult result)
        {
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

        public ArtistResult GetResult()
        {
            return _result;
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