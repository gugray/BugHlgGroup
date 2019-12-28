using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    class Photo
    {
        public int PhotoId;
        public string Description;
        public string PhotoName;
        public string PhotoFileName;
        public DateTime CreatedEastern;
        public string CreatedBy;
        public int Width;
        public int Height;
        public int SizeKB;

        public string LocalFileFullPath;
        public string LocalFileName;
    }
}
