using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVNFOEditor.ViewModels
{
    public class ArtistDetailsBannerViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
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

        public ArtistDetailsBannerViewModel(Bitmap? cover)
        {
            ArtistBanner = cover;
            _parentVM = App.GetVM().GetParentView();
        }

        public void NavigateBack()
        {
            _parentVM.BackToList();
        }
    }
}
