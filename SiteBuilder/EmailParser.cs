using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;

namespace SiteBuilder
{
    class EmailParser
    {
        public Email Parse(string json)
        {
            dynamic jMsg = JsonConvert.DeserializeObject(json);
            Email res = new Email
            {
                MsgId = jMsg.msgId,
                Subject = jMsg.subject ?? "",
                AuthorName = jMsg.authorName,
                From = jMsg.from,
                MsgSnippet = jMsg.msgSnippet,
                TopicId = jMsg.topicId,
                PrevInTopic = jMsg.prevInTopic,
                NextInTopic = jMsg.nextInTopic,
            };
            if (res.AuthorName == "") res.AuthorName = "unknown";
            var rawEmail = jMsg.rawEmail.ToString();
            var rawEmailBytes = Encoding.UTF8.GetBytes(rawEmail);
            var rawEmailStream = new MemoryStream(rawEmailBytes);
            var msg = MimeMessage.Load(rawEmailStream);
            res.TextBody = msg.TextBody;
            res.HtmlBody = msg.HtmlBody;
            res.UtcDateTime = msg.Date.UtcDateTime;
            return res;
        }
    }
}
