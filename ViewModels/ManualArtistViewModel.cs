using Avalonia.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVNFOEditor.ViewModels
{
    public class ManualArtistViewModel : ObservableObject
    {
        private string _artistNameText;
        public string ArtistNameText
        {
            get { return _artistNameText; }
            set
            {
                _artistNameText = value;
                OnPropertyChanged(nameof(ArtistNameText));
            }
        }
        
        private Bitmap? _banner;
        private string _bannerPath;
        
        public Bitmap? ArtistBanner
        {
            get { return _banner; }
            set
            {
                _banner = value;
                OnPropertyChanged(nameof(ArtistBanner));
            }
        }
        public string BannerPath
        {
            get { return _bannerPath; }
            set
            {
                _bannerPath = value;
                OnPropertyChanged(nameof(BannerPath));
            }
        }

        public async void LoadBanner(string path)
        {
            _bannerPath = path;
            ArtistBanner = await Task.Run(() => Bitmap.DecodeToWidth(File.OpenRead(path), 270));
        }
    }
}