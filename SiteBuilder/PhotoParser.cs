using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace SiteBuilder
{
    class PhotoParser
    {
        readonly TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        readonly string photoBasePath;
        readonly DirectoryInfo[] dirs;

        public PhotoParser(string photoBasePath)
        {
            this.photoBasePath = photoBasePath;
            DirectoryInfo di = new DirectoryInfo(photoBasePath);
            dirs = di.GetDirectories();
        }

        public List<Album> ParseAlbums()
        {
            List<Album> res = new List<Album>();
            dynamic jAlbums = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(photoBasePath, "albums.json")));
            foreach (dynamic jAlbum in jAlbums)
            {
                Album album = new Album
                {
                    AlbumId = jAlbum.albumId,
                    AlbumName = jAlbum.albumName,
                    Description = jAlbum.description,
                    CreatedEastern = DateTimeOffset.FromUnixTimeSeconds((long)jAlbum.creationDate).UtcDateTime,
                    CreatedBy = jAlbum.creatorNickname,
                };
                album.CreatedEastern = TimeZoneInfo.ConvertTimeFromUtc(album.CreatedEastern, easternZone);
                album.AlbumName = resolveEntities(album.AlbumName);
                album.Description = resolveEntities(album.Description);
                parsePhotos(album);
                res.Add(album);
            }
            res.Sort((a, b) => b.CreatedEastern.CompareTo(a.CreatedEastern));
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

        void parsePhotos(Album album)
        {
            DirectoryInfo dir = null;
            foreach (var x in dirs)
                if (x.Name.StartsWith(album.AlbumId + "-"))
                    dir = x;
            album.Slug = dir.Name.Substring(dir.Name.IndexOf('-') + 1);
            FileInfo[] files = dir.GetFiles();
            dynamic jPhotos = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(dir.FullName, "photos-0.json")));
            long sizeMBTimesTen = 0;
            long sizeKB = 0;
            foreach (dynamic jPhoto in jPhotos)
            {
                Photo photo = new Photo
                {
                    PhotoId = jPhoto.photoId,
                    PhotoName = jPhoto.photoName,
                    PhotoFileName = jPhoto.photoFilename,
                    Description = jPhoto.description,
                    CreatedEastern = DateTimeOffset.FromUnixTimeSeconds((long)jPhoto.creationDate).UtcDateTime,
                    CreatedBy = jPhoto.creatorNickname,
                };
                photo.CreatedEastern = TimeZoneInfo.ConvertTimeFromUtc(photo.CreatedEastern, easternZone);
                photo.Description = resolveEntities(photo.Description);
                photo.PhotoName = resolveEntities(photo.PhotoName);
                foreach (var x in files)
                    if (x.Name.StartsWith(photo.PhotoId + "-"))
                        photo.LocalFileFullPath = x.FullName;
                foreach (dynamic jPhotoInfo in jPhoto.photoInfo)
                {
                    if (jPhotoInfo.width < photo.Width) continue;
                    photo.Width = jPhotoInfo.width;
                    photo.Height = jPhotoInfo.height;
                }
                var fi = new FileInfo(photo.LocalFileFullPath);
                photo.LocalFileName = fi.Name;
                album.Photos.Add(photo);
                sizeMBTimesTen += fi.Length * 10 / (1024 * 1024);
                photo.SizeKB = (int)fi.Length / 1024;
                sizeKB += photo.SizeKB;
            }
            album.Photos.Sort((a, b) => b.CreatedEastern.CompareTo(a.CreatedEastern));
            album.SizeMB = (decimal)sizeMBTimesTen / 10;
            album.SizeKB = (int)sizeKB;
        }
    }
}
