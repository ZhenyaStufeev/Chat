using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

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
        using Image watermarkImage = Image.FromFile(watermarkImagePath);

        // Calculate the size and position of the watermark
        int watermarkWidth = (int)Math.Round(image.Width * 0.15);
        int watermarkHeight = (int)Math.Round(watermarkImage.Height * ((float)watermarkWidth / watermarkImage.Width));
        int x = image.Width - watermarkWidth - 10;
        int y = image.Height - watermarkHeight - 10;

        // Add the watermark overlay
        using var graphics = Graphics.FromImage(image);
        graphics.DrawImage(watermarkImage, new Rectangle(x, y, watermarkWidth, watermarkHeight),
            0, 0, watermarkImage.Width, watermarkImage.Height, GraphicsUnit.Pixel);

        // Compress the image
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 75L);
        var jpegCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codecInfo => codecInfo.MimeType == "image/jpeg");
        if (jpegCodecInfo != null)
        {
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, jpegCodecInfo, encoderParams);
            using var compressedImage = Image.FromStream(memoryStream);
            // Save the image to disk
            compressedImage.Save(filePath);
        }
        else
        {
            // Save the uncompressed image to disk
            image.Save(filePath);
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
