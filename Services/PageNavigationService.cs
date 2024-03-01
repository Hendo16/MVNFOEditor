using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Features;

namespace MVNFOEditor.Services
{
    public class PageNavigationService
    {
        public Action<Type>? NavigationRequested { get; set; }

        public void RequestNavigation<T>() where T : PageBase
        {
            NavigationRequested?.Invoke(typeof(T));
        }
    }
}