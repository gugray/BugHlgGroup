using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;

namespace SiteBuilder
{
    class EmailParser
    {
        readonly TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        Regex re = new Regex("&.{1,6};");
        HashSet<string> hs = new HashSet<string>();
        int notext = 0;

        static string resolveEntities(string str)
        {
            str = str.Replace("&lt;", "<");
            str = str.Replace("&gt;", ">");
            str = str.Replace("&quot;", "\"");
            str = str.Replace("&apos;", "'");
            str = str.Replace("&#39;", "'");
            str = str.Replace("&#92;", "\\");
            str = str.Replace("&nbsp;", " ");
            str = str.Replace("&amp;", "&");
            return str;
        }

        public Email Parse(string json)
        {
            dynamic jMsg = JsonConvert.DeserializeObject(json);

            // Known spam messages
            if (jMsg.msgId == 1240 || jMsg.msgId == 1211) return null;

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
            if (res.AuthorName == "") res.AuthorName = res.From.Substring(0, res.From.IndexOf('@')).Replace("&lt;", "");
            if (res.Subject == "") res.Subject = "(No subject)";
            res.Subject = resolveEntities(res.Subject);
            string rawEmail = jMsg.rawEmail.ToString();
            //var ms = re.Matches(rawEmail);
            //foreach (var m in ms) hs.Add((m as Match).Value);
            rawEmail = resolveEntities(rawEmail);
            var rawEmailBytes = Encoding.UTF8.GetBytes(rawEmail);
            var rawEmailStream = new MemoryStream(rawEmailBytes);
            var msg = MimeMessage.Load(rawEmailStream);
            res.TextBody = msg.TextBody;
            res.HtmlBody = msg.HtmlBody;
            htmlBodyHacks(res);
            if (res.TextBody == null) ++notext;
            res.UtcDateTime = msg.Date.UtcDateTime;
            res.EasternDateTime = TimeZoneInfo.ConvertTimeFromUtc(res.UtcDateTime, easternZone);
            return res;
        }

        void htmlBodyHacks(Email email)
        {
            if (email.HtmlBody == null) return;
            if (email.HtmlBody.Contains("<body smarttemplateinserted=\"true\" bgcolor=\"#FFFF99\" text=\"#000000\">"))
            {
                email.HtmlBody = email.HtmlBody.Replace("<body smarttemplateinserted=\"true\" bgcolor=\"#FFFF99\" text=\"#000000\">", "");
                email.HtmlBody = email.HtmlBody.Replace("</body>", "");
            }
        }

        void getMultipart(MimeMessage msg, Email res)
        {
            using (var iter = new MimeIterator(msg))
            {
                while (iter.MoveNext())
                {
                    var multipart = iter.Parent as Multipart;
                    var part = iter.Current as MimePart;
                    int jfkds = 0;
                }
            }
        }
    }
}
