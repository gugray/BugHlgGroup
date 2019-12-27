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
        public readonly List<Album> Albums;

        public GroupData(string path)
        {
            parseEmails(path);
            PhotoParser photoParser = new PhotoParser(Path.Combine(path, "photos"));
            Albums = photoParser.ParseAlbums();
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

    }
}
