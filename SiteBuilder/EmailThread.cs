using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    class EmailThread
    {
        public int ThreadId;
        public List<Email> Messages = new List<Email>();
        public Email FirstMessage;
    }
}
