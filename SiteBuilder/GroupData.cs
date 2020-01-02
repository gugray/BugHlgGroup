using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SiteBuilder
{
    class GroupData
    {
        class LunrDoc
        {
            public int id;
            public string subject;
            public string body;
            public string authorName;
            public string date;
        }
        readonly List<LunrDoc> lunrDocs = new List<LunrDoc>();

        public readonly Dictionary<int, Email> IdToEmail = new Dictionary<int, Email>();
        public readonly List<EmailThread> Threads = new List<EmailThread>();
        public readonly List<Album> Albums;
        public readonly List<GroupFileFolder> Files;

        public GroupData(string path, string lunrJsonPath)
        {
            // Emails
            parseEmails(path);
            string lunrJson = JsonConvert.SerializeObject(lunrDocs);
            File.WriteAllText(lunrJsonPath, lunrJson, Encoding.UTF8);
            // Put together threads
            collectThreads();
            // Photos
            PhotoParser photoParser = new PhotoParser(Path.Combine(path, "photos"));
            Albums = photoParser.ParseAlbums();
            // Files
            FilesParser filesParser = new FilesParser(Path.Combine(path, "files"));
            Files = filesParser.ParseFiles();
        }

        void parseEmails(string path)
        {
            EmailParser emailParser = new EmailParser();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(path, "email"));
            var emailFiles = di.GetFiles("*_raw.json");
            foreach (var f in emailFiles)
            {
                string json = File.ReadAllText(f.FullName);
                var email = emailParser.Parse(json);
                if (email == null) continue;
                IdToEmail[email.MsgId] = email;
                lunrDocs.Add(new LunrDoc
                {
                    id = email.MsgId,
                    subject = email.Subject,
                    body = email.TextBody,
                    authorName = email.AuthorName,
                    date = email.EasternDateTime.ToString("MMMM d, yyyy") + " " + email.EasternDateTime.ToShortTimeString(),
                });
            }
        }

        void collectThreads()
        {
            foreach (var email in IdToEmail.Values)
            {
                if (email.PrevInTopic != 0) continue;
                var thread = new EmailThread();
                thread.FirstMessage = email;
                Threads.Add(thread);
            }
            foreach (var thread in Threads)
            {
                var email = thread.FirstMessage;
                while (true)
                {
                    thread.Messages.Add(email);
                    if (email.NextInTopic == 0) break;
                    email = IdToEmail[email.NextInTopic];
                }
                thread.Messages.Sort((a, b) => b.EasternDateTime.CompareTo(a.EasternDateTime));
            }
            Threads.Sort((a, b) => b.FirstMessage.EasternDateTime.CompareTo(a.FirstMessage.EasternDateTime));
            for (int i = 0; i < Threads.Count; ++i)
            {
                Threads[i].ThreadId = Threads.Count - i;
                foreach (var email in Threads[i].Messages) email.Thread = Threads[i];
            }
            Threads.Sort((a, b) => b.Messages[0].EasternDateTime.CompareTo(a.Messages[0].EasternDateTime));
        }
    }
}
