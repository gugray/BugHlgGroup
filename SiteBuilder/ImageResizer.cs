using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SiteBuilder
{
    class ImageResizer
    {
        ImageCodecInfo imageCodecInfo;
        Encoder encoder;
        EncoderParameter encoderParameter;
        EncoderParameters encoderParameters;

        public ImageResizer(int quality)
        {
            imageCodecInfo = getEncoderInfo("image/jpeg");
            encoder = Encoder.Quality;
            encoderParameters = new EncoderParameters(1);
            encoderParameter = new EncoderParameter(encoder, quality);
            encoderParameters.Param[0] = encoderParameter;
        }

        public int Resize(string ifn, string ofn, int newHeight)
        {
            var img = new Bitmap(ifn);
            double height = 48.0;
            double width = height / img.Height * img.Width;
            Size newSize = new Size((int)width, (int)height);
            img = new Bitmap(img, newSize);
            img.Save(ofn, imageCodecInfo, encoderParameters);
            return (int)width;
        }

        static ImageCodecInfo getEncoderInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
