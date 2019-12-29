using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    class Builder
    {
        const string wwwRoot = "../_www";
        const int pageSize = 100;
        const int tinyThumbHeight = 48;
        const int tinyThumbQuality = 90;

        readonly ImageResizer resizer = new ImageResizer(tinyThumbQuality);
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
            // DBG
            buildMessages();
            buildPhotos();
            buildFiles();
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
            // Prev/next
            if (prevId == -1) sb.Replace("{{prev}}", "<span>«</span>");
            else sb.Replace("{{prev}}", "<a href='/messages/" + prevId + "'>«</a>");
            if (nextId == -1) sb.Replace("{{next}}", "<span>»</span>");
            else sb.Replace("{{next}}", "<a href='/messages/" + nextId + "'>»</a>");
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
            string strPage = getPage("messages", "messageView", sb.ToString());

            // Save in regular location
            string path = Path.Combine(wwwRoot, "messages/" + email.MsgId);
            Directory.CreateDirectory(path);
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
        }

        string writeAlbumList(string photosPath)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var album in data.Albums)
            {
                StringBuilder sbItem = new StringBuilder(snips["albumItem"]);
                string countStr = album.Photos.Count.ToString() + " photo";
                if (album.Photos.Count != 1) countStr += "s";
                sbItem.Replace("{{albumLink}}", "/photos/" + album.Slug);
                sbItem.Replace("{{albumName}}", esc(album.AlbumName));
                sbItem.Replace("{{description}}", esc(album.Description));
                sbItem.Replace("{{author}}", esc(album.CreatedBy));
                sbItem.Replace("{{date}}", album.CreatedEastern.ToString("MMMM d, yyyy"));
                sbItem.Replace("{{photoCount}}", countStr);
                if (album.SizeMB >= 1)
                    sbItem.Replace("{{size}}", album.SizeMB.ToString() + "&nbsp;MB");
                else
                    sbItem.Replace("{{size}}", album.SizeKB.ToString() + "&nbsp;KB");
                StringBuilder sbThumbs = new StringBuilder();
                foreach (var photo in album.Photos)
                {
                    string fnThumb = Path.Combine(photosPath, photo.PhotoId + ".jpg");
                    int width = resizer.Resize(photo.LocalFileFullPath, fnThumb, tinyThumbHeight);
                    StringBuilder sbThumb = new StringBuilder(snips["albumTinyThumb"]);
                    sbThumb.Replace("{{photoLink}}", "/photos/" + album.Slug + "/#" + photo.PhotoId);
                    sbThumb.Replace("{{src}}", "/photos/" + photo.PhotoId + ".jpg");
                    sbThumb.Replace("{{width}}", width.ToString());
                    sbThumb.Replace("{{height}}", tinyThumbHeight.ToString());
                    sbThumb.Replace("{{alt}}", esc(photo.PhotoName));
                    sbThumbs.Append(sbThumb);
                }
                sbItem.Replace("{{thumbs}}", sbThumbs.ToString());
                sb.Append(sbItem.ToString());
            }
            return sb.ToString();
        }

        static string prettySizeKB(int kbytes)
        {
            if (kbytes < 1000) return kbytes + " KB";
            int k = kbytes / 1000;
            return k + "," + (kbytes - k * 1000).ToString() + " KB";

        }

        void buildAlbum(Album album, string prevSlug, string nextSlug, string photosPath)
        {
            // Create album folder
            string path = Path.Combine(photosPath, album.Slug);
            Directory.CreateDirectory(path);
            // For each photo: copy file; build list
            StringBuilder sbList = new StringBuilder();
            foreach (var photo in album.Photos)
            {
                File.Copy(photo.LocalFileFullPath, Path.Combine(path, photo.LocalFileName));
                StringBuilder sbPhoto = new StringBuilder(snips["albumPhoto"]);
                sbPhoto.Replace("{{photoId}}", photo.PhotoId.ToString());
                sbPhoto.Replace("{{src}}", "/photos/" + album.Slug + "/" + photo.LocalFileName);
                sbPhoto.Replace("{{width}}", photo.Width.ToString());
                sbPhoto.Replace("{{height}}", photo.Height.ToString());
                sbPhoto.Replace("{{size}}", prettySizeKB(photo.SizeKB));
                sbPhoto.Replace("{{alt}}", esc(photo.PhotoName));
                sbPhoto.Replace("{{photoName}}", esc(photo.PhotoName));
                if (!string.IsNullOrEmpty(photo.Description))
                    sbPhoto.Replace("{{description}}", esc(" · " + photo.Description));
                else
                    sbPhoto.Replace("{{description}}", "");
                sbPhoto.Replace("{{author}}", esc(photo.CreatedBy));
                sbPhoto.Replace("{{date}}", photo.CreatedEastern.ToLongDateString() + " " + photo.CreatedEastern.ToShortTimeString());
                sbList.Append(sbPhoto);
            }
            // Render album content
            StringBuilder sbAlbum = new StringBuilder(snips["album"]);
            sbAlbum.Replace("{{album}}", esc(album.AlbumName));
            sbAlbum.Replace("{{photoList}}", sbList.ToString());
            // Album meta
            if (string.IsNullOrEmpty(album.Description)) sbAlbum.Replace("{{description}}", "n/a");
            else sbAlbum.Replace("{{description}}", esc(album.Description));
            sbAlbum.Replace("{{author}}", esc(album.CreatedBy));
            sbAlbum.Replace("{{date}}", album.CreatedEastern.ToString("MMMM d, yyyy"));
            // Prev/next: pages albums
            if (prevSlug == null) sbAlbum.Replace("{{prev}}", "<span>Prev</span>");
            else sbAlbum.Replace("{{prev}}", "<a href='/photos/" + prevSlug + "'>Prev</a>");
            if (nextSlug == null) sbAlbum.Replace("{{next}}", "<span>Next</span>");
            else sbAlbum.Replace("{{next}}", "<a href='/photos/" + nextSlug + "'>Next</a>");
            // Write page
            string strPage = getPage("photos", "album", sbAlbum.ToString());
            File.WriteAllText(Path.Combine(path, "index.html"), strPage);
        }

        void buildPhotos()
        {
            // Create regular location
            string path = Path.Combine(wwwRoot, "photos");
            Directory.CreateDirectory(path);
            // Albums list
            string strAlbums = "";
            strAlbums = writeAlbumList(path);
            // Main section
            StringBuilder sbAlbums = new StringBuilder(snips["albumList"]);
            // Items
            sbAlbums.Replace("{{items}}", strAlbums);
            // Page; save
            string strPage = getPage("photos", "albumList", sbAlbums.ToString());
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
            // Build individual albums
            for (int i = 0; i < data.Albums.Count; ++i)
            {
                string prevSlug = i < data.Albums.Count - 1 ? data.Albums[i + 1].Slug : null;
                string nextSlug = i > 0 ? data.Albums[i - 1].Slug : null;
                buildAlbum(data.Albums[i], prevSlug, nextSlug, path);
            }
        }

        void buildFiles()
        {
            // Create regular location
            string path = Path.Combine(wwwRoot, "files");
            Directory.CreateDirectory(path);
            // Content
            StringBuilder sbList = new StringBuilder();
            foreach (var folder in data.Files)
                sbList.Append(writeFolder(folder));
            // Page; save
            StringBuilder sbFiles = new StringBuilder(snips["files"]);
            sbFiles.Replace("{{items}}", sbList.ToString());
            string strPage = getPage("files", "files", sbFiles.ToString());
            string fn = Path.Combine(path, "index.html");
            File.WriteAllText(fn, strPage, Encoding.UTF8);
        }

        string writeFolder(GroupFileFolder folder)
        {
            // Create folder for actual files
            string dir = Path.Combine(wwwRoot, "files");
            if (folder.Slug != null)
            {
                dir = Path.Combine(dir, folder.Slug);
                Directory.CreateDirectory(dir);
            }
            // Render page
            StringBuilder sb = new StringBuilder(snips["filesFolder"]);
            sb.Replace("{{folderName}}", esc(folder.Name));
            sb.Replace("{{description}}", esc(folder.Description));
            sb.Replace("{{author}}", esc(folder.CreatedBy));
            sb.Replace("{{date}}", esc(folder.CreatedEastern.ToString("MMMM d, yyyy")));
            string items = "";
            for (int i = 0; i < folder.Files.Count; ++i)
            {
                var gfile = folder.Files[i];
                // Copy file
                File.Copy(gfile.LocalFileFullPath, Path.Combine(dir, gfile.FileName));
                // Render section
                StringBuilder sbFile = new StringBuilder(snips["fileItem"]);
                string href = "/files/";
                if (folder.Slug != "") href += folder.Slug + "/";
                href += gfile.FileName;
                sbFile.Replace("{{fileLink}}", href);
                sbFile.Replace("{{fileName}}", esc(gfile.FileName));
                sbFile.Replace("{{description}}", esc(gfile.Description));
                sbFile.Replace("{{author}}", esc(gfile.CreatedBy));
                sbFile.Replace("{{size}}", prettySizeKB(gfile.SizeKB));
                sbFile.Replace("{{date}}", esc(gfile.CreatedEastern.ToString("MMMM d, yyyy") + " " + gfile.CreatedEastern.ToShortTimeString()));
                items += sbFile.ToString();
            }
            sb.Replace("{{fileList}}", items);
            return sb.ToString();
        }
    }
}
