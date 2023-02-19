using System.Collections.Generic;
using System.Drawing;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public abstract class BaseImageHelper
{
    protected List<(int X, int Y, Color Color)> GetPixels(Bitmap bmp)
    {
        var pixels = new List<(int X, int Y, Color Color)>();

        for (int x = 0; x < bmp.Width; x++)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                Color color = bmp.GetPixel(x, y);
                pixels.Add((x, y, color));
            }
        }

        return pixels;
    }
}

public class ImageHelper : BaseImageHelper
{
    public Bitmap BitmapFromPath(string path)
    {
        Bitmap image = (Bitmap)Image.FromFile(path);
        return image;
    }

    public Color GetColorFromHex(string hex)
    {
        return ColorTranslator.FromHtml(hex);
    }

    public void RemovePixelsByColor(List<Color> colors, Bitmap bmp, double tolerancePercent)
    {
        int totalPixels = bmp.Width * bmp.Height;
        int tolerance = (int)((double)totalPixels * ((double)tolerancePercent / 100.0));

        var pixelsToRemove = new List<(int X, int Y)>();
        var pixels = GetPixels(bmp);

        Parallel.ForEach(pixels, pixel =>
        {
            if (colors.Any(c => IsColorSimilar(c, pixel.Color, tolerance)))
            {
                lock (pixelsToRemove)
                {
                    pixelsToRemove.Add((pixel.X, pixel.Y));
                }
            }
        });

        foreach (var pixel in pixelsToRemove)
        {
            bmp.SetPixel(pixel.X, pixel.Y, Color.Transparent);
        }
    }

    private bool IsColorSimilar(Color c1, Color c2, int tolerance)
    {
        int diffR = Math.Abs(c1.R - c2.R);
        int diffG = Math.Abs(c1.G - c2.G);
        int diffB = Math.Abs(c1.B - c2.B);

        return (diffR * diffR + diffG * diffG + diffB * diffB) <= tolerance;
    }

    public Rectangle FindLargestNonTransparentArea(Bitmap bitmap, int threshold = 0)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                byte* bitmapPtr = (byte*)bitmapData.Scan0;

                Parallel.For(0, height, y =>
                {
                    byte* row = bitmapPtr + (y * bitmapData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        byte blue = row[x * 4];
                        byte green = row[x * 4 + 1];
                        byte red = row[x * 4 + 2];
                        byte alpha = row[x * 4 + 3];

                        if (alpha > threshold)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public Bitmap CropImage(Bitmap originalImage, Rectangle cropRectangle)
    {
        Bitmap croppedImage = new Bitmap(cropRectangle.Width, cropRectangle.Height);
        using (Graphics g = Graphics.FromImage(croppedImage))
        {
            g.DrawImage(originalImage, new Rectangle(0, 0, croppedImage.Width, croppedImage.Height), cropRectangle, GraphicsUnit.Pixel);
        }
        return croppedImage;
    }

    public Bitmap ResizeWithSharpness(Bitmap sourceBitmap, int newWidth, int newHeight)
    {
        Bitmap resultBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

        // Создаем объект Graphics и устанавливаем его свойства
        Graphics graphics = Graphics.FromImage(resultBitmap);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        //graphics.InterpolationMode = InterpolationMode.Bicubic;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Создаем объект Rectangle для задания размера изображения
        Size imageSize = sourceBitmap.Size;
        float aspectRatio = Math.Min((float)newWidth / imageSize.Width, (float)newHeight / imageSize.Height);
        int targetWidth = (int)(imageSize.Width * aspectRatio);
        int targetHeight = (int)(imageSize.Height * aspectRatio);
        int targetX = (newWidth - targetWidth) / 2;
        int targetY = (newHeight - targetHeight) / 2;

        // Применяем фильтр Лапласа и изменяем размер изображения
        graphics.DrawImage(sourceBitmap, new Rectangle(targetX, targetY, targetWidth, targetHeight));

        graphics.Dispose();

        return resultBitmap;
    }
}

public class ImageLoader
{
    // Метод для рекурсивной загрузки и обработки изображений
    public static void ProcessImages(string sourceFolder, string targetFolder, Action<Image, string, string> imageProcessor)
    {
        // Получаем все файлы из папки sourceFolder
        var files = Directory.GetFiles(sourceFolder);

        foreach (var file in files)
        {
            // Если файл - изображение, то обрабатываем его
            if (IsImageFile(file))
            {
                // Загружаем изображение
                using (var image = Image.FromFile(file))
                {
                    // Вызываем переданный метод для обработки изображения
                    imageProcessor(image, file, targetFolder);
                }
            }
        }

        // Рекурсивно обходим все подпапки
        var folders = Directory.GetDirectories(sourceFolder);
        foreach (var folder in folders)
        {
            // Создаем соответствующую папку в целевой директории
            var subfolder = Path.Combine(targetFolder, Path.GetFileName(folder));
            Directory.CreateDirectory(subfolder);

            // Рекурсивно обходим все файлы и подпапки в текущей подпапке
            ProcessImages(folder, subfolder, imageProcessor);
        }
    }

    // Метод для проверки, является ли файл изображением
    private static bool IsImageFile(string file)
    {
        var extension = Path.GetExtension(file).ToLower();
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp";
    }
}
