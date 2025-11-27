using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public class ArtistResultViewModel : ObservableObject
{
    private readonly ArtistResult _result;
    private Bitmap? _thumbnail;

    internal ArtistResultViewModel(ArtistResult result)
    {
        _result = result;
    }

    public static async Task<ArtistResultViewModel> CreateViewModel(ArtistResult result)
    {
        ArtistResultViewModel newVm = new ArtistResultViewModel(result);
        await newVm.LoadThumbnail();
        return newVm;
    }

    public string? Name => _result.Name;

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
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
}