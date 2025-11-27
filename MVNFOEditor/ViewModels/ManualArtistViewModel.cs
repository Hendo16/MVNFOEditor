using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVNFOEditor.ViewModels;

public class ManualArtistViewModel : ObservableObject
{
    private string _artistNameText;

    private Bitmap? _banner;
    private string _bannerPath;

    public string ArtistNameText
    {
        get => _artistNameText;
        set
        {
            _artistNameText = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? ArtistBanner
    {
        set
        {
            _banner = value;
            OnPropertyChanged();
        }
    }
}