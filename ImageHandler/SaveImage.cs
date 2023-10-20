using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageHandler
{
    public class ImageHandler
    {
        private static readonly string imageFolderPath = @"C:\Users\Zhenya\Desktop\FolderImage\SavedImages";
        private static readonly string watermarkImagePath = @"C:\Users\Zhenya\Desktop\FolderImage\watermark.png";

        public static string SaveImage(Image image, string fileName)
        {
            string filePath = Path.Combine(imageFolderPath, fileName);

            // Ensure the file name is unique
            int counter = 1;
            while (File.Exists(filePath))
            {
                string extension = Path.GetExtension(fileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                fileName = $"{fileNameWithoutExtension}_{counter++}{extension}";
                filePath = Path.Combine(imageFolderPath, fileName);
            }

            // Load the watermark image
            using (Image watermarkImage = Image.FromFile(watermarkImagePath))
            {
                // Calculate the size and position of the watermark
                int watermarkWidth = (int)Math.Round(image.Width * 0.15);
                int watermarkHeight = (int)Math.Round(watermarkImage.Height * ((float)watermarkWidth / watermarkImage.Width));
                int x = image.Width - watermarkWidth - 10;
                int y = image.Height - watermarkHeight - 10;

                // Create a new bitmap with the same dimensions as the original image
                using (Bitmap bitmapWithWatermark = new Bitmap(image.Width, image.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmapWithWatermark))
                    {
                        // Draw the original image on the new bitmap
                        graphics.DrawImage(image, 0, 0, image.Width, image.Height);

                        // Draw the watermark on the new bitmap
                        graphics.DrawImage(watermarkImage, new Rectangle(x, y, watermarkWidth, watermarkHeight),
                            0, 0, watermarkImage.Width, watermarkImage.Height, GraphicsUnit.Pixel);
                    }

                    // Compress the image
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 75L);
                    var jpegCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codecInfo => codecInfo.MimeType == "image/jpeg");
                    if (jpegCodecInfo != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            bitmapWithWatermark.Save(memoryStream, jpegCodecInfo, encoderParams);
                            // Save the image to disk
                            File.WriteAllBytes(filePath, memoryStream.ToArray());
                        }
                    }
                    else
                    {
                        // Save the uncompressed image to disk
                        bitmapWithWatermark.Save(filePath);
                    }
                }
            }

            return filePath;
        }

        public static Image LoadImage(string filePath)
        {
            // Load the image from disk
            var image = Image.FromFile(filePath);
            return image;
        }
    }
}