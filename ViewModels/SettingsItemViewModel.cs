using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;
using System;

namespace MVNFOEditor.ViewModels
{
    public partial class SettingsItemViewModel : ObservableObject, ISukiStackPageTitleProvider
    {
        public string Title { get; }

        private readonly int _index;
        private readonly Action<SettingsItemViewModel> _onRecurseClicked;

        public SettingsItemViewModel(int index, Action<SettingsItemViewModel> onRecurseClicked)
        {
            _index = index;
            _onRecurseClicked = onRecurseClicked;
            Title = $"Take Me Back To Eden {index}";
        }

        [RelayCommand]
        public void Recurse() =>
            _onRecurseClicked.Invoke(new SettingsItemViewModel(_index + 1, _onRecurseClicked));
    }
}