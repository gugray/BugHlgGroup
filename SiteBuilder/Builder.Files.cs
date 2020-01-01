using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    partial class Builder
    {
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
            string strPage = getPage("files", "files", sbFiles.ToString(), titleFilesPage);
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
