using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.Models;
using SukiUI.Toasts;
using YtMusicNet.Records;

namespace MVNFOEditor.ViewModels;

public partial class AddArtistSourceViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddArtistSourceViewModel));
    private Artist currentArtist;
    private SearchSource selectedSource;
    [ObservableProperty] private ArtistResultsViewModel _resultsVM;
    [ObservableProperty] private bool _ytEnabled = true;
    [ObservableProperty] private bool _amEnabled = true;
    [ObservableProperty] private bool _ytChecked;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private string _errorText;
    
    public AddArtistSourceViewModel(Artist existingArtist)
    {
        currentArtist = existingArtist;
        SearchSource? newSource = SetupSources();
        if (newSource == null)
        {
            App.GetToastManager().CreateToast()
                .OfType(NotificationType.Error)
                .WithContent($"Error: Artist has no new source options")
                .Dismiss().ByClicking()
                .Queue();
            CloseDialog();
            return;
        }
        ResultsVM = new ArtistResultsViewModel((SearchSource)newSource, existingArtist.Name);
    }

    public SearchSource? SetupSources()
    {
        YtEnabled = currentArtist.Metadata.All(meta => meta.SourceId != SearchSource.YouTubeMusic);
        AmEnabled = currentArtist.Metadata.All(meta => meta.SourceId != SearchSource.AppleMusic);
        if (!YtEnabled && !AmEnabled)
        {
            return null;
        }
        //Handle UI so that the right radio button looks 'checked'
        selectedSource = YtEnabled ? SearchSource.YouTubeMusic : SearchSource.AppleMusic;
        switch (selectedSource)
        {
            case SearchSource.YouTubeMusic:
                YtChecked = true;
                break;
            case SearchSource.AppleMusic:
                AmChecked = true;
                break;
        }
        return selectedSource;
    }
    
    public void YouTubeChecked()
    {
        ResultsVM.selectedSource = SearchSource.YouTubeMusic;
        ResultsVM.SearchResults.Clear();
    }
    public void AppleMusicChecked()
    {
        ResultsVM.selectedSource = SearchSource.AppleMusic;
        ResultsVM.SearchResults.Clear();
    }
    public async void SaveSource()
    {
        var resultCard = ResultsVM.SelectedArtist.GetResult();
        ArtistMetadata? newMetadata = await ArtistMetadata.GetNewMetadata(resultCard, currentArtist);
        App.GetDBContext().ArtistMetadata.Add(newMetadata);
        await App.GetDBContext().SaveChangesAsync();
        App.GetVM().GetParentView().RefreshList();
        CloseDialog();
    }
    
    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}