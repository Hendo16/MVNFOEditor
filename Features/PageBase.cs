using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using MVNFOEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Features
{
    public abstract partial class PageBase(string displayName, MaterialIconKind icon, int index = 0) : ObservableValidator
    {
        [ObservableProperty] private string _displayName = displayName;
        [ObservableProperty] private MaterialIconKind _icon = icon;
        [ObservableProperty] private int _index = index;
    }
}