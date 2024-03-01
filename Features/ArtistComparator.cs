using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;

namespace MVNFOEditor.Features
{
    public class ArtistComparator : IEqualityComparer<Artist>
    {
        public bool Equals(Artist? art1, Artist? art2)
        {
            return String.Equals(art1.Name, art2.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] Artist obj)
        {
            return base.GetHashCode();
        }
    }
}
