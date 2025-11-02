using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace MVNFOEditor.ViewModels;

public partial class ArtistViewModel : ObservableObject
{
    private readonly Artist _artist;
    [ObservableProperty] private Bitmap? _cover;
    [ObservableProperty] private int _coverWidth;
    [ObservableProperty] private Bitmap? _largeBanner;
    [ObservableProperty] private List<Bitmap> _sourceIcons = new();
    [ObservableProperty] private bool _sourcesAvailable;

    public ArtistViewModel(Artist artist)
    {
        _artist = artist;
        //Bit hacky, would love something more dynamic but we're likely only ever going to have 2 sources anyway...
        SourcesAvailable =
            artist.Metadata.Count(am => am.SourceId is SearchSource.YouTubeMusic or SearchSource.AppleMusic) == 1;
        CoverWidth = 540 / 2;
    }

    public string Name => _artist.Name;

    public static async Task<List<ArtistViewModel>> GenerateViewModels(IEnumerable<Artist> artists)
    {
        var artistsModels = new List<ArtistViewModel>();
        for (var i = 0; i < artists.Count(); i++)
        {
            var artVM = new ArtistViewModel(artists.ElementAt(i));
            await artVM.LoadCover();
            artVM.LoadSourceIcons();
            artistsModels.Add(artVM);
        }

        return artistsModels;
    }

    private void LoadSourceIcons()
    {
        try
        {
            foreach (var metadata in _artist.Metadata)
            {
                var path = metadata.SourceIconPath;
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
        var newVM = new AddArtistSourceViewModel(_artist);
        newVM.RefreshArtistCard += RefreshIcons;
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => newVM)
            .TryShow();
    }

    public void RefreshIcons(object? sender, bool t)
    {
        SourceIcons.Clear();
        LoadSourceIcons();
    }

    public async void EditArtist()
    {
        var editVM = new EditArtistDialogViewModel(_artist);
        editVM.RefreshArtistCard += RefreshIcons;
        await editVM.LoadBanners();
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => editVM)
            .TryShow();
    }

    public async Task LoadCover()
    {
        if (_artist.CardBannerURL != null)
            await using (var imageStream = await _artist.LoadCardBannerBitmapAsync())
            {
                Cover = Bitmap.DecodeToWidth(imageStream, 540);
            }
        else
            await using (var imageStream = await _artist.LoadLocalCardBannerBitmapAsync())
            {
                Cover = Bitmap.DecodeToWidth(imageStream, 540);
            }
    }

    public void HandleArtistClick()
    {
        var defaultVM = App.GetVM().GetParentView();
        var artDetailsVM = new ArtistDetailsViewModel();
        artDetailsVM.SetArtist(_artist);
        defaultVM.SetDetailsVM(artDetailsVM);
        defaultVM.CurrentContent = artDetailsVM;
    }
}