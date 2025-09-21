using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels;

public partial class YTMHeaderSubmissionViewModel(ISukiDialog dialog) : ObservableObject
{
    [ObservableProperty] private string _headers;
    [ObservableProperty] private string _errorText;
    
    [RelayCommand]
    private void GenerateHeaders()
    {
        ErrorText = "";
        bool valid = App.GetYTMusicHelper().SetupBrowserHeaders(Headers);
        if (valid)
        {
            //Need to re-create YTM as an Authenticated Instance with the new headers
            App.RefreshYTMusicHelper();
            dialog.Dismiss();
        }
        else
        {
            ErrorText = "Error generating headers, please check logs.";
        }
        
    }

    [RelayCommand]
    private void CloseDialog() => dialog.Dismiss();
}