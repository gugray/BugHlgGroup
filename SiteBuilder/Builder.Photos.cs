using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SiteBuilder
{
    partial class Builder
    {
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
            string title = string.Format(titlePhotoPage, album.AlbumName);
            string strPage = getPage("photos", "album", sbAlbum.ToString(), title, "photos/" + album.Slug);
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
            string strPage = getPage("photos", "albumList", sbAlbums.ToString(), titlePhotosPage, "photos");
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
    }
}
