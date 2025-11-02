using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels;

public partial class SettingsItemViewModel : ObservableObject, ISukiStackPageTitleProvider
{
    private readonly int _index;
    private readonly Action<SettingsItemViewModel> _onRecurseClicked;

    public SettingsItemViewModel(int index, Action<SettingsItemViewModel> onRecurseClicked)
    {
        _index = index;
        _onRecurseClicked = onRecurseClicked;
        Title = $"Take Me Back To Eden {index}";
    }

    public string Title { get; }

    [RelayCommand]
    public void Recurse()
    {
        _onRecurseClicked.Invoke(new SettingsItemViewModel(_index + 1, _onRecurseClicked));
    }
}