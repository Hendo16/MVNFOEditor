using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;

namespace MVNFOEditor.ViewModels
{
    public partial class MusicVideoListParentViewModel : PageBase
    {
        private MusicVideoListViewModel _musicVideoList;
        private MusicVideoDetailsViewModel _musicVideoDetails;
        private MusicDBHelper _dbHelper;

        [ObservableProperty] private object _currentContent;

        public MusicVideoListParentViewModel() : base("Music Videos", MaterialIconKind.AccountMusic, 1)
        {
            MusicVideoListViewModel currView = new MusicVideoListViewModel();
            CurrentContent = currView;
            _musicVideoList = currView;
            _dbHelper = App.GetDBHelper();
        }

        public void SetDetailsVM(MusicVideoDetailsViewModel vm)
        {
            _musicVideoDetails = vm;
        }


    }
}
