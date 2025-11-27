using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using YtMusicNet.Models;

namespace MVNFOEditor.ViewModels;

public partial class ArtistDetailsBannerViewModel : ObservableObject
{
    private readonly ArtistDetailsViewModel _detailsVM;

    private readonly ArtistListParentViewModel _parentVM;
    [ObservableProperty] private Bitmap? _artistBanner;

    [ObservableProperty] private bool _hasAppleMusic;

    //Show/Hide Source Options
    [ObservableProperty] private bool _hasYTMusic;
    [ObservableProperty] private List<Bitmap> _sourceIcons = new();
    [ObservableProperty] private bool _sourcesAvailable;

    public ArtistDetailsBannerViewModel(Bitmap? cover, ArtistDetailsViewModel vm)
    {
        ArtistBanner = cover;
        _parentVM = App.GetVM().GetParentView();
        _detailsVM = vm;
        //Bit hacky, would love something more dynamic but we're likely only ever going to have 2 sources anyway...
        SourcesAvailable =
            _detailsVM.Artist.Metadata.Count(am =>
                am.SourceId is SearchSource.YouTubeMusic or SearchSource.AppleMusic) == 1;
        foreach (var metadata in vm.Artist.Metadata)
        {
            var path = metadata.SourceIconPath;
            SourceIcons.Add(new Bitmap(path));
            if (metadata.SourceId == SearchSource.YouTubeMusic) HasYTMusic = true;
            if (metadata.SourceId == SearchSource.AppleMusic) HasAppleMusic = true;
        }
    }

    public void AddSource()
    {
        var newVM = new AddArtistSourceViewModel(_detailsVM.Artist);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => newVM)
            .TryShow();
    }

    public void AddAlbum()
    {
        _detailsVM.AddResult(typeof(AlbumResult));
    }

    public void AddVideo()
    {
        _detailsVM.AddResult(typeof(VideoResult));
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