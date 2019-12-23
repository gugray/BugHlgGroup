using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    class Builder
    {
        const string wwwRoot = "../_www";
        const int pageSize = 100;
        readonly GroupData data;
        readonly Dictionary<string, string> snips = new Dictionary<string, string>();

        public Builder(GroupData data)
        {
            this.data = data;
            foreach (var f in new DirectoryInfo("./src").GetFiles("*.html"))
            {
                var fn = f.Name.Replace(".html", "");
                snips[fn] = File.ReadAllText(f.FullName);
            }
        }

        public void Build()
        {
            recursiveDelete(new DirectoryInfo(wwwRoot));
            buildMessageList();
            buildMessages();
        }

        static string esc(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c == '<') sb.Append("&lt;");
                else if (c == '>') sb.Append("&gt;");
                else if (c == '&') sb.Append("&amp;");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        static void recursiveDelete(DirectoryInfo baseDir, bool isRoot = true)
        {
            foreach (var dir in baseDir.EnumerateDirectories())
                recursiveDelete(dir, false);
            if (!isRoot) baseDir.Delete(true);
            else
            {
                foreach (var file in baseDir.EnumerateFiles())
                    file.Delete();
            }
        }

        string getPage(string menu, string mainClass, string mainContent)
        {
            StringBuilder sb = new StringBuilder(snips["index"]);
            // Menu state
            if (menu == "messages") sb.Replace("{{navMessagesClass}}", "selected");
            else sb.Replace("{{navMessagesClass}}", "");
            if (menu == "threads") sb.Replace("{{navThreadsClass}}", "selected");
            else sb.Replace("{{navThreadsClass}}", "");
            if (menu == "photos") sb.Replace("{{navPhotosClass}}", "selected");
            else sb.Replace("{{navPhotosClass}}", "");
            if (menu == "files") sb.Replace("{{navFilesClass}}", "selected");
            else sb.Replace("{{navFilesClass}}", "");
            if (menu == "about") sb.Replace("{{navAboutClass}}", "selected");
            else sb.Replace("{{navAboutClass}}", "");
            // Main
            sb.Replace("{{mainClass}}", mainClass);
            sb.Replace("{{mainContent}}", mainContent);
            return sb.ToString();
        }

        void buildMessageList()
        {
            List<int> ids = new List<int>();
            ids.AddRange(data.IdToEmail.Keys);
            ids.Sort((a, b) => b.CompareTo(a));
            StringBuilder sbList = new StringBuilder();
            int page = 1;
            int pageCount = ids.Count / pageSize + 1;
            for (int i = 0; i < ids.Count; ++i)
            {
                Email email = data.IdToEmail[ids[i]];
                StringBuilder sbItem = new StringBuilder(snips["messageListItem"]);
                sbItem.Replace("{{msgLink}}", "/messages/" + email.MsgId);
                sbItem.Replace("{{subject}}", esc(email.Subject));
                sbItem.Replace("{{snippet}}", esc(email.MsgSnippet));
                sbItem.Replace("{{author}}", esc(email.AuthorName));
                sbItem.Replace("{{date}}", esc(email.EasternDateTime.ToString("MMMM d, yyyy") + " " + email.EasternDateTime.ToShortTimeString()));
                sbList.Append(sbItem);
                if (i % pageSize == 0 && i > 0)
                {
                    writeMessageListPage(page, pageCount, sbList.ToString());
                    sbList.Clear();
                    ++page;
                }
            }
            if (sbList.Length > 0) writeMessageListPage(page, pageCount, sbList.ToString());
        }

        string buildPageLinks(int page, int pageCount, string urlFormat)
        {
            StringBuilder sb = new StringBuilder();
            if (page == 1) sb.Append("<span>«</span> <span class='selected'>1</span> ");
            else
            {
                sb.Append("<a href='" + string.Format(urlFormat, page - 1) + "'>«</a> ");
                sb.Append("<a href='" + string.Format(urlFormat, 1) + "'>1</a> ");
            }
            if (page > 3) sb.Append("<span>…</span> ");
            int i = page - 1;
            if (i < 2) i = 2;
            while (i <= page + 1 && i < pageCount)
            {
                if (i != page) sb.Append("<a href='" + string.Format(urlFormat, i) + "'>" + i + "</a> ");
                else sb.Append("<span class='selected'>" + i + "</span> ");
                ++i;
            }
            if (i < pageCount) sb.Append("<span>…</span> ");
            if (page == pageCount) sb.Append("<span class='selected'>" + pageCount + "</span> <span>»</span> ");
            else
            {
                sb.Append("<a href='" + string.Format(urlFormat, pageCount) + "'>" + pageCount + "</a> ");
                sb.Append("<a href='" + string.Format(urlFormat, page + 1) + "'>»</a> ");
            }
            return sb.ToString();
        }

        void writeMessageListPage(int page, int pageCount, string strListItems)
        {
            // Main section
            StringBuilder sbList = new StringBuilder(snips["messageList"]);
            // Navigation
            sbList.Replace("{{pageNav}}", buildPageLinks(page, pageCount, "/messages/page-{0}"));
            // Items
            sbList.Replace("{{items}}", strListItems);
            // Page
            string strPage = getPage("messages", "messageList", sbList.ToString());
            // Save in regular location
            string path = Path.Combine(wwwRoot, "messages/page-" + page);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
            // If it's page 1, then we also store in /messages and /
            if (page == 1)
            {
                path = wwwRoot;
                File.WriteAllText(Path.Combine(path, "index.html"), strPage, Encoding.UTF8);
                path = Path.Combine(path, "messages");
                File.WriteAllText(Path.Combine(path, "index.html"), strPage, Encoding.UTF8);
            }
        }

        void buildMessages()
        {
            List<int> ids = new List<int>();
            ids.AddRange(data.IdToEmail.Keys);
            ids.Sort((a, b) => b.CompareTo(a));
            for (int i = 0; i < ids.Count; ++i)
            {
                Email email = data.IdToEmail[ids[i]];
                writeMessage(email, i > 0 ? ids[i - 1] : -1, i < ids.Count - 1 ? ids[i + 1] : -1);
            }
        }

        readonly Regex reUrl = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=;]*)?");

        string decorateLinks(string plainEncoded)
        {
            var ms = reUrl.Matches(plainEncoded);
            if (ms.Count == 0) return plainEncoded;
            List<Match> msList = new List<Match>();
            foreach (Match m in ms) msList.Add(m);
            msList.Sort((a, b) => b.Index.CompareTo(a.Index));
            foreach (Match m in msList)
            {
                string hrefStr = "<a href='" + m.Value.Replace("&amp;", "&") + "'>" + m.Value + "</a>";
                plainEncoded =
                    plainEncoded.Substring(0, m.Index) +
                    hrefStr +
                    plainEncoded.Substring(m.Index + m.Length);
            }
            return plainEncoded;
        }

        void writeMessage(Email email, int prevId, int nextId)
        {
            // Render message
            StringBuilder sb = new StringBuilder(snips["message"]);
            sb.Replace("{{msgId}}", email.MsgId.ToString());
            // Pre/next
            if (prevId == -1) sb.Replace("{{prev}}", "<span>«</span>");
            else sb.Replace("{{prev}}", "<a href='/messages/" + prevId + "'>«</a>");
            if (nextId == -1) sb.Replace("{{next}}", "<span>»</span>");
            else sb.Replace("{{next}}", "<a href='/messages/" + nextId + "'>»</a>");
            // Meta
            sb.Replace("{{subject}}", email.Subject.ToString());
            sb.Replace("{{from}}", email.From);
            sb.Replace("{{date}}", email.EasternDateTime.ToLongDateString() + " " + email.EasternDateTime.ToShortTimeString());
            if (email.MsgId == 1038)
            {
                int jfds = 0;
            }
            // Body
            if (email.HtmlBody != null)
            {
                sb.Replace("{{bodyClass}}", "msgHtml");
                sb.Replace("{{body}}", email.HtmlBody);
            }
            else
            {
                sb.Replace("{{bodyClass}}", "msgPlain");
                string body = email.TextBody ?? "";
                body = body.TrimStart();
                body = esc(body);
                body = decorateLinks(body);
                sb.Replace("{{body}}", body);
            }

            // Assemble page
            string strPage = getPage("messages", "messageView", sb.ToString());

            // Save in regular location
            string path = Path.Combine(wwwRoot, "messages/" + email.MsgId);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
        }
    }
}
