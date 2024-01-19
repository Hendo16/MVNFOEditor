using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string title { get; set; }
        public string year { get; set; }
        public string artURL { get; set; }
    }
}