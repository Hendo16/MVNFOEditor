using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.DB;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public class AlbumResultViewModel : ObservableObject
{
    private readonly AlbumResult _result;
    private string _borderColor;
    private MusicDbContext _dbContext;
    private Bitmap? _thumbnail;

    internal AlbumResultViewModel(AlbumResult result)
    {
        _result = result;
    }

    public static async Task<AlbumResultViewModel> CreateAsync(AlbumResult result)
    {
        AlbumResultViewModel vm = new AlbumResultViewModel(result);
        await vm.LoadThumbnail();
        return vm;
    }

    public string Title => _result.Name;
    public string? Year => _result.Year;
    public bool? IsExplicit => _result.IsExplicit;

    public string BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<AlbumResult> NextPage;

    public AlbumResult GetResult()
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

    protected virtual void TriggerNextPage()
    {
        NextPage?.Invoke(this, _result);
    }
}