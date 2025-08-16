using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace MVNFOEditor.ViewModels
{
    public class ArtistViewModel : ObservableObject
    {
        private readonly Artist _artist;

        public ArtistViewModel(Artist artist)
        {
            _artist = artist;
        }

        public string Name => _artist.Name;

        private Bitmap? _cover;
        private Bitmap? _largeBanner;

        public string SourceIcon
        {
            get
            {
                switch (_artist.Metadata.First().SourceId)
                {
                    case SearchSource.YouTubeMusic:
                        return "./Assets/ytm-48x48.png";
                    case SearchSource.AppleMusic:
                        return "./Assets/am-48x48.png";
                    default:
                        return "";
                }
            }
        }
        
        public Bitmap? Cover
        {
            get { return _cover; }
            set
            {
                _cover = value;
                OnPropertyChanged(nameof(Cover));
            }
        }

        public Bitmap? LargeBanner
        {
            get { return _largeBanner; }
            set
            {
                _largeBanner = value;
                OnPropertyChanged(nameof(LargeBanner));
            }
        }

        public void EditArtist()
        {
            EditArtistDialogViewModel editVM = new EditArtistDialogViewModel(_artist, Cover);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => editVM)
                .TryShow();
        }

        public async Task LoadCover()
        {
            if ( _artist.CardBannerURL != null)
            {
                await using (var imageStream = await _artist.LoadCardBannerBitmapAsync())
                {
                    Cover = Bitmap.DecodeToWidth(imageStream, 540);
                }
            }
            else
            {
                await using (var imageStream = await _artist.LoadLocalCardBannerBitmapAsync())
                {
                    Cover = Bitmap.DecodeToWidth(imageStream, 540);
                }
            }
        }

        public async Task LoadLargeBanner()
        {
            if (_artist.LargeBannerURL != null)
            {
                await using (var imageStream = await _artist.LoadLargeBannerBitmapAsync())
                {
                    LargeBanner = Bitmap.DecodeToHeight(imageStream, 800);
                }
            }
            else
            {
                await using (var imageStream = await _artist.LoadLocalLargeBannerBitmapAsync())
                {
                    LargeBanner = Bitmap.DecodeToHeight(imageStream, 800);
                }
            }
        }

        public async Task SaveCoverAsync()
        {
            var bitmap = Cover;
            if (bitmap != null)
            {
                await Task.Run(() =>
                {
                    using (var fs = _artist.SaveCardBannerBitmapStream())
                    {
                        bitmap.Save(fs);
                    }
                });
            }
        }

        public async Task SaveLargeBannerAsync()
        {
            var bitmap = LargeBanner;
            if (bitmap != null)
            {
                await Task.Run(() =>
                {
                    using (var fs = _artist.SaveLargeBannerBitmapStream())
                    {
                        bitmap.Save(fs);
                    }
                });
            }
        }

        public void HandleArtistClick()
        {
            var defaultVM = App.GetVM().GetParentView();
            ArtistDetailsViewModel artDetailsVM = new ArtistDetailsViewModel();
            artDetailsVM.SetArtist(_artist);
            defaultVM.SetDetailsVM(artDetailsVM);
            defaultVM.CurrentContent = artDetailsVM;
        }
    }
}