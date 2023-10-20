using System.Collections.Generic;
using System.Drawing;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;

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
        var pixelsToRemove = new ConcurrentBag<(int X, int Y)>();
        var pixels = GetPixels(bmp);

        Parallel.For(0, pixels.Count, pixelIdx =>
        {
            var pixel = pixels[pixelIdx];
            if (colors.Any(c => IsColorSimilar(c, pixel.Color, tolerance)))
            {
                pixelsToRemove.Add((pixel.X, pixel.Y));
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

    public Rectangle FindLargestNonTransparentArea(Bitmap bitmap, int threshold = 0, int minClusterSize = 1)
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

                // Find clusters of non-transparent pixels
                List<List<Point>> clusters = new List<List<Point>>();
                bool[,] visited = new bool[width, height];
                for (int y = 0; y < height; y++)
                {
                    byte* row = bitmapPtr + (y * bitmapData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        byte alpha = row[x * 4 + 3];

                        if (alpha > threshold && !visited[x, y])
                        {
                            List<Point> cluster = new List<Point>();
                            FindCluster(bitmapPtr, x, y, width, height, threshold, visited, cluster);
                            clusters.Add(cluster);
                        }
                    }
                }

                // Find the largest non-transparent area
                foreach (List<Point> cluster in clusters)
                {
                    if (cluster.Count >= minClusterSize)
                    {
                        int clusterMinX = cluster.Min(p => p.X);
                        int clusterMaxX = cluster.Max(p => p.X);
                        int clusterMinY = cluster.Min(p => p.Y);
                        int clusterMaxY = cluster.Max(p => p.Y);

                        if (clusterMinX < minX) minX = clusterMinX;
                        if (clusterMaxX > maxX) maxX = clusterMaxX;
                        if (clusterMinY < minY) minY = clusterMinY;
                        if (clusterMaxY > maxY) maxY = clusterMaxY;
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private unsafe void FindCluster(byte* bitmapPtr, int x, int y, int width, int height, int threshold, bool[,] visited, List<Point> cluster)
    {
        Queue<Point> queue = new Queue<Point>();
        queue.Enqueue(new Point(x, y));

        while (queue.Count > 0)
        {
            Point point = queue.Dequeue();
            x = point.X;
            y = point.Y;

            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y])
                continue;

            visited[x, y] = true;

            byte alpha = bitmapPtr[(y * width + x) * 4 + 3];

            if (alpha > threshold)
            {
                cluster.Add(new Point(x, y));

                queue.Enqueue(new Point(x - 1, y));
                queue.Enqueue(new Point(x + 1, y));
                queue.Enqueue(new Point(x, y - 1));
                queue.Enqueue(new Point(x, y + 1));
            }
        }
    }

    public Bitmap CropImage(Bitmap originalImage, Rectangle cropRectangle)
    {
        if (originalImage == null)
        {
            throw new ArgumentNullException(nameof(originalImage));
        }

        if (cropRectangle.Width <= 0 || cropRectangle.Height <= 0)
        {
            throw new ArgumentException("Invalid cropRectangle dimensions");
        }

        // Убедитесь, что cropRectangle находится в пределах границ originalImage
        cropRectangle.Intersect(new Rectangle(0, 0, originalImage.Width, originalImage.Height));

        if (cropRectangle.Width <= 0 || cropRectangle.Height <= 0)
        {
            throw new ArgumentException("Invalid cropRectangle dimensions within the boundaries of the originalImage");
        }

        Bitmap croppedImage = new Bitmap(cropRectangle.Width, cropRectangle.Height);
        using (Graphics g = Graphics.FromImage(croppedImage))
        {
            g.DrawImage(originalImage, new Rectangle(0, 0, croppedImage.Width, croppedImage.Height), cropRectangle, GraphicsUnit.Pixel);
        }
        return croppedImage;
    }

    public Bitmap ResizeWithSharpness(Bitmap sourceBitmap, int newWidth, int newHeight, InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
    {
        Bitmap resultBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

        // Создаем объект Graphics и устанавливаем его свойства
        Graphics graphics = Graphics.FromImage(resultBitmap);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = interpolationMode;
        //graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
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
