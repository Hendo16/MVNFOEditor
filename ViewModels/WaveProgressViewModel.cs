using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class WaveProgressViewModel : ObservableObject
    {
        private double _progressValue;
        private string _headerText;
        public double ProgressValue
        {
            get {return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }
        public string HeaderText
        {
            get { return _headerText; }
            set
            {
                _headerText = value;
                OnPropertyChanged(nameof(HeaderText));
            }
        }

        public WaveProgressViewModel()
        {
        }

        public void UpdateProgress(float value)
        {
            var newValue = value * 100;
            if (newValue > ProgressValue)
            {
                ProgressValue = newValue;
            }
        }
    }
}
