using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    class GroupFileFolder
    {
        public string Name;
        public string Description;
        public string Slug;
        public DateTime CreatedEastern;
        public string CreatedBy;
        public List<GroupFile> Files = new List<GroupFile>();
    }
}
