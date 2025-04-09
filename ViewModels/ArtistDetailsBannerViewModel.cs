using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistDetailsBannerViewModel : ObservableObject
    {
        //Show/Hide Source Options
        [ObservableProperty] private bool _isYTMusic;
        [ObservableProperty] private bool _isAppleMusic;
        
        private ArtistListParentViewModel _parentVM;
        private ArtistDetailsViewModel _detailsVM;
        private Bitmap? _artistBanner;
        public Bitmap? ArtistBanner
        {
            get { return _artistBanner; }
            set
            {
                _artistBanner = value;
                OnPropertyChanged(nameof(ArtistBanner));
            }
        }

        public ArtistDetailsBannerViewModel(Bitmap? cover, ArtistDetailsViewModel vm)
        {
            ArtistBanner = cover;
            _parentVM = App.GetVM().GetParentView();
            _detailsVM = vm;

            _isYTMusic = vm.Source == SearchSource.YouTubeMusic;
            _isAppleMusic = vm.Source == SearchSource.AppleMusic;
        }

        public void AddAlbum()
        {
            _detailsVM.AddAlbum();
        }

        public void AddYTMVideo()
        {
            _detailsVM.AddYTMVideo();
        }

        public void AddAppleMusicVideo()
        {
            _detailsVM.AddAppleMusicVideo();
        }

        public void AddManualVideo()
        {
            _detailsVM.AddManualVideo();
        }

        public void NavigateBack()
        {
            _parentVM.BackToList();
        }
    }
}