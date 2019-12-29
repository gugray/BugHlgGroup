using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SiteBuilder
{
    class FilesParser
    {
        const string rootName = "Unsorted files";
        const string rootDescription = "Uploaded directly into the group's files section";
        const string rootCreatedBy = "gldrgidr";

        readonly TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        readonly string filesBasePath;
        readonly DirectoryInfo[] dirs;

        public FilesParser(string filesBasePath)
        {
            this.filesBasePath = filesBasePath;
            DirectoryInfo di = new DirectoryInfo(filesBasePath);
            dirs = di.GetDirectories();
        }

        public List<GroupFileFolder> ParseFiles()
        {
            List<GroupFileFolder> res;
            GroupFileFolder root = new GroupFileFolder();
            parseFolder(filesBasePath, root, out res);
            root.Name = rootName;
            root.Description = rootDescription;
            root.CreatedBy = rootCreatedBy;
            root.CreatedEastern = root.Files[root.Files.Count - 1].CreatedEastern;
            for (int i = 0; i < res.Count; ++i)
            {
                List<GroupFileFolder> subsubs;
                parseFolder(dirs[i].FullName, res[i], out subsubs);
            }
            res.Sort((a, b) => b.CreatedEastern.CompareTo(a.CreatedEastern));
            // Root is first
            res.Insert(0, root);
            return res;
        }

        static string resolveEntities(string str)
        {
            if (str == null) return str;
            str = str.Replace("&lt;", "<");
            str = str.Replace("&gt;", ">");
            str = str.Replace("&quot;;", "\""); // Yes! Sic!
            str = str.Replace("&quot;", "\"");
            str = str.Replace("&apos;", "'");
            str = str.Replace("&#39;;", "'"); // Yes! Sic!
            str = str.Replace("&#39;", "'");
            str = str.Replace("&#92;", "\\");
            str = str.Replace("&nbsp;", " ");
            str = str.Replace("&amp;", "&");
            return str;
        }

        void parseFolder(string dataPath, GroupFileFolder res, out List<GroupFileFolder> subs)
        {
            var dirsHere = new DirectoryInfo(dataPath).GetDirectories();
            var filesHere = new DirectoryInfo(dataPath).GetFiles();
            subs = new List<GroupFileFolder>();
            int ix = 0;
            dynamic jFiles = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(dataPath, "fileinfo.json")));
            foreach (dynamic jFile in jFiles)
            {
                ++ix;
                if (jFile.description == "Clipboard") continue;
                // This is a folder
                if (jFile.mimeType == "")
                {
                    GroupFileFolder sub = new GroupFileFolder
                    {
                        Name = resolveEntities((string)jFile.fileName),
                        Description = resolveEntities((string)jFile.description),
                        CreatedEastern = DateTimeOffset.FromUnixTimeSeconds((long)jFile.createdTime).UtcDateTime,
                        CreatedBy = resolveEntities((string)jFile.ownerName),
                    };
                    foreach (var di in dirsHere)
                    {
                        if (di.Name.StartsWith(ix.ToString() + "_"))
                            sub.Slug = di.Name.Substring(di.Name.IndexOf("_") + 1);
                    }
                    subs.Add(sub);
                }
                else
                {
                    GroupFile gfile = new GroupFile
                    {
                        Description = resolveEntities((string)jFile.description),
                        CreatedEastern = DateTimeOffset.FromUnixTimeSeconds((long)jFile.createdTime).UtcDateTime,
                        CreatedBy = resolveEntities((string)jFile.ownerName),
                    };
                    foreach (var fi in filesHere)
                    {
                        if (fi.Name.StartsWith(ix.ToString() + "_"))
                        {
                            gfile.LocalFileFullPath = fi.FullName;
                            gfile.FileName = fi.Name.Substring(fi.Name.IndexOf("_") + 1);
                            gfile.SizeKB = (int)(fi.Length / 1024);
                            if (gfile.SizeKB == 0) gfile.SizeKB = 1;
                        }
                    }
                    res.Files.Add(gfile);
                }
            }
            res.Files.Sort((a, b) => b.CreatedEastern.CompareTo(a.CreatedEastern));
        }
    }
}
