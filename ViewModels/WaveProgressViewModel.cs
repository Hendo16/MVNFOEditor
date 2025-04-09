using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using NUnit.Framework.Internal;

namespace MVNFOEditor.ViewModels
{
    public class WaveProgressViewModel : ObservableObject
    {
        private double _progressValue;
        private string? _headerText;
        public double ProgressValue
        {
            get {return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }
        public string? HeaderText
        {
            get { return _headerText; }
            set
            {
                _headerText = value;
                OnPropertyChanged(nameof(HeaderText));
            }
        }

        public void UpdateDownloadSpeed(string speed)
        {
            string main_header = HeaderText.Split(" - ")[0];
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
            {
                ProgressValue = newValue;
            }
            else if (value == 0)
            {
                ProgressValue = 0;
            }
        }

        public void UpdateProgress(float value, ProgressBar progTest)
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
}
