using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainViewModel MainView { get; } = new MainViewModel();
        public ArtistViewModel ArtistView { get; } = new ArtistViewModel();
        

        public void AddVideo()
        {
            //New song

        }
    }
}
