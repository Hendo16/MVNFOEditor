using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVNFOEditor.ViewModels;

public partial class WaveProgressViewModel : ObservableObject
{
    [ObservableProperty] public bool _isTextVisible;
    [ObservableProperty] private bool _circleVisible;
    private string? _headerText;
    [ObservableProperty] public bool _isIndeterminate;
    private double _progressValue;
    [ObservableProperty] private bool _waveVisible;

    public WaveProgressViewModel(bool isIndeterminate = false, bool waveVisible = false, bool circleVisible = false)
    {
        IsIndeterminate = isIndeterminate;
        CircleVisible = circleVisible;
        WaveVisible = waveVisible;
    }

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged();
        }
    }

    public string? HeaderText
    {
        get => _headerText;
        set
        {
            _headerText = value;
            OnPropertyChanged();
        }
    }

    public void UpdateDownloadSpeed(string speed)
    {
        var main_header = HeaderText.Split(" - ")[0];
        HeaderText = $"{main_header} - {speed}";
    }

    public void UpdateProgress(double value)
    {
        ProgressValue = value;
    }

    public void UpdateProgress(float value)
    {
        var newValue = value * 100;
        if (newValue > ProgressValue)
            ProgressValue = newValue;
        else if (value == 0) ProgressValue = 0;
    }

    public void UpdateProgress(float value, ref ProgressBar progTest)
    {
        var newValue = value * 100;
        if (newValue > ProgressValue)
        {
            ProgressValue = newValue;
            progTest.Value = newValue;
        }
        else if (value == 0)
        {
            ProgressValue = 0;
            progTest.Value = 0;
        }
    }
}