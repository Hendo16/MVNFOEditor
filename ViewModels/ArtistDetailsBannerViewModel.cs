using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistDetailsBannerViewModel : ObservableObject
    {
        //Show/Hide Source Options
        [ObservableProperty] private bool _hasYTMusic;
        [ObservableProperty] private bool _hasAppleMusic;
        [ObservableProperty] private bool _sourcesAvailable;
        [ObservableProperty] private Bitmap? _artistBanner;
        [ObservableProperty] private List<Bitmap> _sourceIcons = new();
        
        private ArtistListParentViewModel _parentVM;
        private ArtistDetailsViewModel _detailsVM;

        public ArtistDetailsBannerViewModel(Bitmap? cover, ArtistDetailsViewModel vm)
        {
            ArtistBanner = cover;
            _parentVM = App.GetVM().GetParentView();
            _detailsVM = vm;
            //Bit hacky, would love something more dynamic but we're likely only ever going to have 2 sources anyway...
            SourcesAvailable = _detailsVM.Artist.Metadata.Count(am => am.SourceId is SearchSource.YouTubeMusic or SearchSource.AppleMusic) == 1;
            foreach (var metadata in vm.Artist.Metadata)
            {
                string path = metadata.SourceIconPath;
                SourceIcons.Add(new Bitmap(path));
                if (metadata.SourceId == SearchSource.YouTubeMusic)
                {
                    HasYTMusic = true;
                }
                if (metadata.SourceId == SearchSource.AppleMusic)
                {
                    HasAppleMusic = true;
                }
            }
        }

        public void AddSource()
        {
            AddArtistSourceViewModel newVM = new AddArtistSourceViewModel(_detailsVM.Artist);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(_ => newVM)
                .TryShow();
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