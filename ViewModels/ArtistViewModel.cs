using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistViewModel : ObservableObject
    {
        [ObservableProperty] private bool _sourcesAvailable;
        [ObservableProperty] private Bitmap? _cover;
        [ObservableProperty] private Bitmap? _largeBanner;
        [ObservableProperty] private List<Bitmap> _sourceIcons;
        private readonly Artist _artist;

        public ArtistViewModel(Artist artist)
        {
            _artist = artist;
            SourceIcons = new List<Bitmap>();
            //Bit hacky, would love something more dynamic but we're likely only ever going to have 2 sources anyway...
            SourcesAvailable = artist.Metadata.Count(am => am.SourceId is SearchSource.YouTubeMusic or SearchSource.AppleMusic) == 1;
        }

        public async static Task<List<ArtistViewModel>> GenerateViewModels(IEnumerable<Artist> artists)
        {
            List<ArtistViewModel> artistsModels = new List<ArtistViewModel>();
            for (int i = 0; i < artists.Count(); i++)
            {
                ArtistViewModel artVM =  new ArtistViewModel(artists.ElementAt(i));
                await artVM.LoadCover();
                artVM.LoadSourceIcons();
                artistsModels.Add(artVM);
            }

            return artistsModels;
        }

        public string Name => _artist.Name;
        
        public void LoadSourceIcons()
        {
            try
            {
                foreach (var metadata in _artist.Metadata)
                {
                    string path = metadata.SourceIconPath;
                    SourceIcons.Add(new Bitmap(path));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void AddSource()
        {
            AddArtistSourceViewModel newVM = new AddArtistSourceViewModel(_artist);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(_ => newVM)
                .TryShow();
        }

        public void EditArtist()
        {
            EditArtistDialogViewModel editVM = new EditArtistDialogViewModel(_artist, Cover);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(_ => editVM)
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