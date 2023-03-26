using Chat;
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

        private static List<Color> getGreenColors(ImageHelper imageHelper)
        {
            List<Color> colors = new List<Color>();
            colors.Add(imageHelper.GetColorFromHex("#2d9700"));
            colors.Add(imageHelper.GetColorFromHex("#4bfc00"));
            colors.Add(imageHelper.GetColorFromHex("#3cca00"));
            return colors;
        }

        private static List<Color> getOrangeColors(ImageHelper imageHelper)
        {
            List<Color> colors = new List<Color>();
            colors.Add(imageHelper.GetColorFromHex("#978000"));
            colors.Add(imageHelper.GetColorFromHex("#fcd500"));
            colors.Add(imageHelper.GetColorFromHex("#caab00"));
            return colors;
        }

        private static void RunProcessImages(HandlerSettings settings)
        {
            ImageHelper imageHelper = settings.InstanceImageHelper;
            ImageLoader.ProcessImages("C:\\Users\\Zhenya\\Desktop\\Screen\\Green", "C:\\Users\\Zhenya\\Desktop\\Screen\\Green-done", (image, filePath, targetFolder) =>
            {
                using (var newImage = imageHelper.BitmapFromPath(filePath))
                {
                    if (settings.IsTransparent)
                        newImage.MakeTransparent();

                    Bitmap bitmap = newImage;
                    if (settings.RemoveBackground == true)
                    {
                        imageHelper.RemovePixelsByColor(settings.Colors, newImage, settings.Tolerance);
                        Rectangle cropRectangle = imageHelper.FindLargestNonTransparentArea(newImage, settings.FindNonTransparent);
                        bitmap = imageHelper.CropImage(newImage, cropRectangle);
                    }

                    if (settings.NeedResize == true)
                    {
                        bitmap = imageHelper.ResizeWithSharpness(bitmap, settings.CropSizeX, settings.CropSizeY);
                    }

                    // Создаем путь для сохранения изображения в целевой директории
                    var subfolder = Path.GetDirectoryName(filePath).Substring(Path.GetDirectoryName(filePath).IndexOf(Path.DirectorySeparatorChar) + 1);
                    var targetPath = Path.Combine(targetFolder, Path.GetFileName(filePath));

                    CreateDirectoryIfNotExists(Path.GetDirectoryName(targetPath));

                    // Сохраняем изображение в новую папку с сохранением структуры папок
                    bitmap.Save(targetPath);
                }
            });
        }

        static void Main(string[] args)
        {
            HandlerSettings GreenSettings = new HandlerSettings()
            {
                NeedResize = false,
                RemoveBackground = true,
                IsTransparent = true,
                CropSizeX = 300,
                CropSizeY = 300,
                Tolerance = 0.05,
                FindNonTransparent = 254,
                SourcePath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Green",
                ResultPath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Green-done"
            };
            GreenSettings.Colors = getGreenColors(GreenSettings.InstanceImageHelper);
            RunProcessImages(GreenSettings);

            //HandlerSettings OrangeSettings = new HandlerSettings()
            //{
            //    NeedResize = false,
            //    RemoveBackground = true,
            //    CropSizeX = 300,
            //    CropSizeY = 300,
            //    Tolerance = 0.05,
            //    FindNonTransparent = 250,
            //    SourcePath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Orange",
            //    ResultPath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Orange-done"
            //};
            //OrangeSettings.Colors = getOrangeColors(OrangeSettings.InstanceImageHelper);
            //RunProcessImages(OrangeSettings);
        }
    }
}
