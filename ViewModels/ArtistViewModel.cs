using System.Diagnostics;
using System.Linq;
using MVNFOEditor.Models;
using ReactiveUI;
using System.Threading.Tasks;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public class ArtistViewModel : ReactiveObject
    {
        private readonly Artist _artist;

        public ArtistViewModel(Artist artist)
        {
            _artist = artist;
        }

        public string Name => _artist.Name;

        private Bitmap? _cover;
        private Bitmap? _largeBanner;

        public Bitmap? Cover
        {
            get => _cover;
            private set => this.RaiseAndSetIfChanged(ref _cover, value);
        }

        public Bitmap? LargeBanner
        {
            get => _largeBanner;
            private set => this.RaiseAndSetIfChanged(ref _largeBanner, value);
        }

        public void EditArtist()
        {
            EditArtistDialogViewModel editVM = new EditArtistDialogViewModel(_artist, Cover);
            SukiHost.ShowDialog(editVM, allowBackgroundClose: true);
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