using Chat;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            colors.Add(imageHelper.GetColorFromHex("#973f00"));
            colors.Add(imageHelper.GetColorFromHex("#ca5400"));
            return colors;
        }

        private static List<Color> getOrangeBrown(ImageHelper imageHelper)
        {
            List<Color> colors = new List<Color>();
            colors.Add(imageHelper.GetColorFromHex("#26170d"));
            colors.Add(imageHelper.GetColorFromHex("#41220d"));
            colors.Add(imageHelper.GetColorFromHex("#2d1a0d"));
            colors.Add(imageHelper.GetColorFromHex("#40220d"));
            colors.Add(imageHelper.GetColorFromHex("#371e0d"));
            colors.Add(imageHelper.GetColorFromHex("#1c130c"));
            colors.Add(imageHelper.GetColorFromHex("#22150c"));
            colors.Add(imageHelper.GetColorFromHex("#171717"));
            return colors;
        }

        private static List<Color> getDarkGreen(ImageHelper imageHelper)
        {
            List<Color> colors = new List<Color>();
            colors.Add(imageHelper.GetColorFromHex("#13240d"));
            colors.Add(imageHelper.GetColorFromHex("#17310d"));
            colors.Add(imageHelper.GetColorFromHex("#13230d"));
            colors.Add(imageHelper.GetColorFromHex("#14250c"));
            return colors;
        }

        private static void RunProcessImages(HandlerSettings settings)
        {
            ImageHelper imageHelper = settings.InstanceImageHelper;
            ImageLoader.ProcessImages(settings.SourcePath, settings.ResultPath, (image, filePath, targetFolder) =>
            {
                Task.Run(() =>
                {
                    using (var newImage = imageHelper.BitmapFromPath(filePath))
                    {
                        if (settings.IsTransparent)
                            newImage.MakeTransparent();

                        Bitmap bitmap = newImage;
                        if (settings.RemoveBackground == true)
                        {
                            imageHelper.RemovePixelsByColor(settings.Colors, newImage, settings.Tolerance);
                            Rectangle cropRectangle = imageHelper.FindLargestNonTransparentArea(newImage, settings.FindNonTransparent, settings.MinClusterSize);
                            bitmap = imageHelper.CropImage(newImage, cropRectangle);
                        }

                        if (settings.NeedResize == true)
                        {
                            bitmap = imageHelper.ResizeWithSharpness(bitmap, settings.CropSizeX, settings.CropSizeY, settings.Interpolation);
                        }

                        // Создаем путь для сохранения изображения в целевой директории
                        var subfolder = Path.GetDirectoryName(filePath).Substring(Path.GetDirectoryName(filePath).IndexOf(Path.DirectorySeparatorChar) + 1);
                        var targetPath = Path.Combine(targetFolder, Path.GetFileName(filePath));

                        CreateDirectoryIfNotExists(Path.GetDirectoryName(targetPath));

                        // Сохраняем изображение в новую папку с сохранением структуры папок
                        bitmap.Save(targetPath);
                    }
                });
            });
        }

        static void Main(string[] args)
        {
            HandlerSettings GreenSettings = new HandlerSettings()
            {
                NeedResize = false,
                RemoveBackground = true,
                IsTransparent = true,
                MinClusterSize = 5,
                CropSizeX = 300,
                CropSizeY = 300,
                Tolerance = 0.05,
                Interpolation = InterpolationMode.NearestNeighbor,
                FindNonTransparent = 254,
                SourcePath = "C:\\Users\\Zhenya\\Desktop\\FigmaProj\\Chat",
                ResultPath = "C:\\Users\\Zhenya\\Desktop\\FigmaProj\\Chat-done"
            };
            GreenSettings.Colors = getGreenColors(GreenSettings.InstanceImageHelper);
            RunProcessImages(GreenSettings);

            //HandlerSettings DarkGreenSettings = new HandlerSettings()
            //{
            //    NeedResize = false,
            //    RemoveBackground = true,
            //    IsTransparent = true,
            //    MinClusterSize = 5,
            //    CropSizeX = 400,
            //    CropSizeY = 400,
            //    Tolerance = 0.05,
            //    Interpolation = InterpolationMode.NearestNeighbor,
            //    FindNonTransparent = 254,
            //    SourcePath = "C:\\Users\\Zhenya\\Desktop\\Screen\\DarkGreen",
            //    ResultPath = "C:\\Users\\Zhenya\\Desktop\\Screen\\DarkGreen-done"
            //};
            //DarkGreenSettings.Colors = getDarkGreen(DarkGreenSettings.InstanceImageHelper);
            //RunProcessImages(DarkGreenSettings);

            //HandlerSettings OrangeSettings = new HandlerSettings()
            //{
            //    NeedResize = true,
            //    RemoveBackground = true,
            //    IsTransparent = true,
            //    MinClusterSize = 5,
            //    CropSizeX = 400,
            //    CropSizeY = 400,
            //    Tolerance = 0.05,
            //    FindNonTransparent = 250,
            //    SourcePath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Orange",
            //    ResultPath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Orange-done"
            //};
            //OrangeSettings.Colors = getOrangeColors(OrangeSettings.InstanceImageHelper);
            //RunProcessImages(OrangeSettings);

            //HandlerSettings BrownSettings = new HandlerSettings()
            //{
            //    NeedResize = false,
            //    RemoveBackground = true,
            //    IsTransparent = true,
            //    MinClusterSize = 5,
            //    CropSizeX = 300,
            //    CropSizeY = 300,
            //    Tolerance = 0.015,
            //    FindNonTransparent = 200,
            //    SourcePath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Brown",
            //    ResultPath = "C:\\Users\\Zhenya\\Desktop\\Screen\\Brown-done"
            //};
            //BrownSettings.Colors = getOrangeBrown(BrownSettings.InstanceImageHelper);
            //RunProcessImages(BrownSettings);

            Console.ReadLine();
        }
    }
}
