using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageProcessing
{
    class Program
    {
        private static void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        static void Main(string[] args)
        {
            ImageLoader.ProcessImages("C:\\Users\\Zhenya\\Desktop\\FolderImage\\EA Guide", "C:\\Users\\Zhenya\\Desktop\\FolderImage\\EA Guide-done", (image, filePath, targetFolder) =>
            {
                var imageHelper = new ImageHelper();
                List<Color> colors = new List<Color>();
                colors.Add(imageHelper.GetColorFromHex("#4bfc00"));
                //colors.Add(imageHelper.GetColorFromHex("#2d9700"));

                using (var newImage = imageHelper.BitmapFromPath(filePath))
                {
                    newImage.MakeTransparent();

                    imageHelper.RemovePixelsByColor(colors, newImage, 0.8);

                    Rectangle cropRectangle = imageHelper.FindLargestNonTransparentArea(newImage, 200);

                    var cropedBitmap = imageHelper.CropImage(newImage, cropRectangle);

                    var result = imageHelper.ResizeWithSharpness(cropedBitmap, 300, 300);

                    // Создаем путь для сохранения изображения в целевой директории
                    var subfolder = Path.GetDirectoryName(filePath).Substring(Path.GetDirectoryName(filePath).IndexOf(Path.DirectorySeparatorChar) + 1);
                    var targetPath = Path.Combine(targetFolder, Path.GetFileName(filePath));

                    CreateDirectoryIfNotExists(Path.GetDirectoryName(targetPath));

                    // Сохраняем изображение в новую папку с сохранением структуры папок
                    result.Save(targetPath);
                }
            });
        }
    }
}
