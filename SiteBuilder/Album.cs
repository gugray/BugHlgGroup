using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    class Album
    {
        public int AlbumId;
        public string AlbumName;
        public string Description;
        public DateTime CreatedEastern;
        public string CreatedBy;
        public string Slug;
        public List<Photo> Photos = new List<Photo>();
    }
}
