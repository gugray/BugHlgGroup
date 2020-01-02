using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    partial class Builder
    {
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

        void writeMessageListPage(int page, int pageCount, string strListItems)
        {
            // Main section
            StringBuilder sbList = new StringBuilder(snips["messageList"]);
            // Navigation
            sbList.Replace("{{pageNav}}", buildPageLinks(page, pageCount, "/messages/page-{0}"));
            // Items
            sbList.Replace("{{items}}", strListItems);
            // Page
            string title = string.Format(titleMessagesPage, page);
            string relPath = string.Format("messages/page-{0}", page);
            string strPage = getPage("messages", "messageList", sbList.ToString(), title, relPath);
            // Save in regular location
            string path = Path.Combine(wwwRoot, "messages/page-" + page);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
            // If it's page 1, then we also store in /messages and /
            if (page == 1)
            {
                path = Path.Combine(wwwRoot, "messages");
                File.WriteAllText(Path.Combine(path, "index.html"), strPage, Encoding.UTF8);
                // Home gets different title
                strPage = getPage("messages", "messageList", sbList.ToString(), titleHome, "messages");
                path = wwwRoot;
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
                writeMessage(email,
                    i > 0 ? ids[i - 1] : -1,
                    i < ids.Count - 1 ? ids[i + 1] : -1);
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
            // Prev/next
            if (prevId == -1) sb.Replace("{{prev}}", "<span>«</span>");
            else sb.Replace("{{prev}}", "<a href='/messages/" + prevId + "'>«</a>");
            if (nextId == -1) sb.Replace("{{next}}", "<span>»</span>");
            else sb.Replace("{{next}}", "<a href='/messages/" + nextId + "'>»</a>");
            // Full thread link
            sb.Replace("{{threadHref}}", "/threads/" + email.Thread.ThreadId);
            sb.Replace("{{threadLength}}", email.Thread.Messages.Count.ToString());
            if (email.Thread.Messages.Count == 1) sb.Replace("{{threadLinkClass}}", "hidden");
            else sb.Replace("{{threadLinkClass}}", "");
            // Meta
            sb.Replace("{{subject}}", email.Subject.ToString());
            sb.Replace("{{from}}", email.From);
            sb.Replace("{{date}}", email.EasternDateTime.ToLongDateString() + " " + email.EasternDateTime.ToShortTimeString());
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
            string title = string.Format(titleMessagePage, email.Subject);
            string strPage = getPage("messages", "messageView", sb.ToString(), title, "messages/" + email.MsgId);

            // Save in regular location
            string path = Path.Combine(wwwRoot, "messages/" + email.MsgId);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
        }

        void buildThreadList()
        {
            StringBuilder sbList = new StringBuilder();
            int page = 1;
            int pageCount = data.Threads.Count / pageSize + 1;
            for (int i = 0; i < data.Threads.Count; ++i)
            {
                EmailThread thread = data.Threads[i];
                StringBuilder sbItem = new StringBuilder(snips["threadListItem"]);
                sbItem.Replace("{{count}}", thread.Messages.Count.ToString());
                sbItem.Replace("{{threadLink}}", "/threads/" + thread.ThreadId);
                sbItem.Replace("{{subject}}", esc(thread.FirstMessage.Subject));
                sbItem.Replace("{{snippet}}", esc(thread.FirstMessage.MsgSnippet));
                sbItem.Replace("{{firstAuthor}}", esc(thread.FirstMessage.AuthorName));
                sbItem.Replace("{{firstDate}}", esc(thread.FirstMessage.EasternDateTime.ToString("MMMM d, yyyy") + " " + thread.FirstMessage.EasternDateTime.ToShortTimeString()));
                sbItem.Replace("{{latestAuthor}}", esc(thread.Messages[0].AuthorName));
                sbItem.Replace("{{latestDate}}", esc(thread.Messages[0].EasternDateTime.ToString("MMMM d, yyyy") + " " + thread.Messages[0].EasternDateTime.ToShortTimeString()));
                if (thread.Messages.Count == 1) sbItem.Replace("{{latestClass}}", "hidden");
                else sbItem.Replace("{{latestClass}}", "");
                sbList.Append(sbItem);
                if (i % pageSize == 0 && i > 0)
                {
                    writeThreadListPage(page, pageCount, sbList.ToString());
                    sbList.Clear();
                    ++page;
                }
            }
            if (sbList.Length > 0) writeThreadListPage(page, pageCount, sbList.ToString());
        }

        void writeThreadListPage(int page, int pageCount, string strListItems)
        {
            // Main section
            StringBuilder sbList = new StringBuilder(snips["threadList"]);
            // Navigation
            sbList.Replace("{{pageNav}}", buildPageLinks(page, pageCount, "/threads/page-{0}"));
            // Items
            sbList.Replace("{{items}}", strListItems);
            // Page
            string title = string.Format(titleThreadsPage, page);
            string strPage = getPage("threads", "threadsList", sbList.ToString(), title, "threads/page-" + page);
            // Save in regular location
            string path = Path.Combine(wwwRoot, "threads/page-" + page);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
            // If it's page 1, then we also store in /threads
            if (page == 1)
            {
                path = Path.Combine(wwwRoot, "threads");
                File.WriteAllText(Path.Combine(path, "index.html"), strPage, Encoding.UTF8);
            }
        }

        void writeThread(EmailThread thread, int prevId, int nextId)
        {
            // Render thread
            StringBuilder sb = new StringBuilder(snips["thread"]);
            sb.Replace("{{threadId}}", thread.ThreadId.ToString());
            sb.Replace("{{subject}}", esc(thread.FirstMessage.Subject));
            // Prev/next
            if (prevId == -1) sb.Replace("{{prev}}", "<span>«</span>");
            else sb.Replace("{{prev}}", "<a href='/threads/" + prevId + "'>«</a>");
            if (nextId == -1) sb.Replace("{{next}}", "<span>»</span>");
            else sb.Replace("{{next}}", "<a href='/threads/" + nextId + "'>»</a>");

            // Render each message in thread
            StringBuilder sbMessages = new StringBuilder();
            for (int i = 0; i < thread.Messages.Count; ++i)
            {
                var email = thread.Messages[i];
                StringBuilder sbMessage = new StringBuilder(snips["threadMessage"]);
                // Meta
                sbMessage.Replace("{{subject}}", email.Subject.ToString());
                sbMessage.Replace("{{from}}", email.From);
                sbMessage.Replace("{{date}}", email.EasternDateTime.ToLongDateString() + " " + email.EasternDateTime.ToShortTimeString());
                if (i == 0) sbMessage.Replace("{{articleClass}}", "first");
               else sbMessage.Replace("{{articleClass}}", "");
                // Body
                if (email.HtmlBody != null)
                {
                    sbMessage.Replace("{{bodyClass}}", "msgHtml");
                    sbMessage.Replace("{{body}}", email.HtmlBody);
                }
                else
                {
                    sbMessage.Replace("{{bodyClass}}", "msgPlain");
                    string body = email.TextBody ?? "";
                    body = body.TrimStart();
                    body = esc(body);
                    body = decorateLinks(body);
                    sbMessage.Replace("{{body}}", body);
                }
                sbMessages.Append(sbMessage);
            }
            sb.Replace("{{messages}}", sbMessages.ToString());

            // Assemble page
            string title = string.Format(titleThreadPage, thread.FirstMessage.Subject);
            string strPage = getPage("threads", "threadView", sb.ToString(), title, "threads/" + thread.ThreadId);

            // Save in regular location
            string path = Path.Combine(wwwRoot, "threads/" + thread.ThreadId);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
        }

        void buildThreads()
        {
            for (int i = 0; i < data.Threads.Count; ++i)
            {
                EmailThread thread = data.Threads[i];
                writeThread(thread,
                    i > 0 ? data.Threads[i - 1].ThreadId : -1, 
                    i < data.Threads.Count - 1 ? data.Threads[i + 1].ThreadId : -1);
            }
        }
    }
}
