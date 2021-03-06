﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    partial class Builder
    {
        const string wwwRoot = "../_www";
        const string baseUrl = "https://bug-hlg.jealousmarkup.xyz/";
        const int pageSize = 100;
        const int tinyThumbHeight = 48;
        const int tinyThumbQuality = 90;
        const string titleHome = "Bug HLG - Mailing List Archive";
        const string titleMessagesPage = "Messages - Page {0} - Bug HLG";
        const string titleMessagePage = "{0} - Message - Bug HLG";
        const string titleThreadsPage = "Threads - Page {0} - Bug HLG";
        const string titleThreadPage = "{0} - Thread - Bug HLG";
        const string titleFilesPage = "Files - Bug HLG";
        const string titlePhotosPage = "Photos - Bug HLG";
        const string titlePhotoPage = "{0} - Album - Bug HLG";

        readonly ImageResizer resizer = new ImageResizer(tinyThumbQuality);
        readonly GroupData data;
        readonly Dictionary<string, string> snips = new Dictionary<string, string>();
        readonly List<string> paths = new List<string>();

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
            buildThreadList();
            buildThreads();
            buildPhotos();
            buildFiles();
            buildContent();
            writeSitemap();
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

        string getPage(string menu, string mainClass, string mainContent, string title, string relPath, bool noIndex = false)
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
            // Title, relative path
            sb.Replace("{{title}}", esc(title).Replace("\"", "&quot;"));
            sb.Replace("{{relpath}}", relPath);
            // Noindex
            if (noIndex) sb.Replace("{{othermeta}}", "<meta name='robots' content='noindex,nofollow' />");
            else sb.Replace("{{othermeta}}", "");
            // Main
            sb.Replace("{{mainClass}}", mainClass);
            sb.Replace("{{mainContent}}", mainContent);
            return sb.ToString();
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

        static string prettySizeKB(int kbytes)
        {
            if (kbytes < 1000) return kbytes + " KB";
            int k = kbytes / 1000;
            return k + "," + (kbytes - k * 1000).ToString() + " KB";
        }

        void buildContent()
        {
            paths.Add("about");
            string[] pages = new string[] { "about", "publisher" };
            foreach (var pg in pages)
            {
                // Create regular location
                string path = Path.Combine(wwwRoot, pg);
                Directory.CreateDirectory(path);
                // Page; save
                StringBuilder sbContent = new StringBuilder(snips[pg]);
                bool noIndex = pg == "publisher";
                string strPage = getPage(pg, "content", sbContent.ToString(), titleFilesPage, pg, noIndex);
                string fn = Path.Combine(path, "index.html");
                File.WriteAllText(fn, strPage, Encoding.UTF8);
            }
        }

        void writeSitemap()
        {
            var fn = Path.Combine(wwwRoot, "sitemap.txt");
            using (FileStream fs = new FileStream(fn, FileMode.Create, FileAccess.ReadWrite))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII))
            {
                sw.NewLine = "\n";
                foreach (var x in paths)
                {
                    sw.WriteLine(baseUrl + x);
                }
            }
        }
    }
}
