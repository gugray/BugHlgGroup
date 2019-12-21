using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    class Email
    {
        public int MsgId;
        public string Subject;
        public string AuthorName;
        public string From;
        public string MsgSnippet;
        public DateTime UtcDateTime;
        public DateTime EasternDateTime;
        public string HtmlBody;
        public string TextBody;
        public int TopicId;
        public int NextInTopic;
        public int PrevInTopic;
    }
}
