using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
#pragma warning disable CS0657 // Not a valid attribute location for this declaration

namespace MVNFOEditor.ViewModels;

public partial class NFODetails : ObservableObject
{
    [ObservableProperty] [property: Category("NFO Tags"), DisplayName("Title")]
    private string _title = String.Empty;

    [ObservableProperty] [property: Category("NFO Tags"), DisplayName("Year")]
    private int? _year;
    
    [ObservableProperty] [property: Category("NFO Tags"), DisplayName("Source")]
    private Album _album;
    
    [ObservableProperty] [property: Category("NFO Tags"), DisplayName("Source")]
    private SearchSource _source;
}