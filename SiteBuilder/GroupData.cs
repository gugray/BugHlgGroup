using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SiteBuilder
{
    class GroupData
    {
        public readonly Dictionary<int, Email> IdToEmail = new Dictionary<int, Email>();
        public readonly List<EmailThread> Threads = new List<EmailThread>();
        public readonly List<Album> Albums;
        public readonly List<GroupFileFolder> Files;

        public GroupData(string path)
        {
            parseEmails(path);
            collectThreads();
            PhotoParser photoParser = new PhotoParser(Path.Combine(path, "photos"));
            Albums = photoParser.ParseAlbums();
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
