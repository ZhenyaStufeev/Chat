using ImageHandler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class Program
    {
        private static void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static List<Color> GetColors(ImageHelper imageHelper, PresetColor presetColor)
        {
            List<Color> colors = presetColor.Colors.Select(c => imageHelper.GetColorFromHex(c)).ToList();
            return colors;
        }
        private static async Task<bool> RunProcessImagesAsync(HandlerSettings settings)
        {
            ImageHelper imageHelper = settings.InstanceImageHelper;
            List<Task> tasks = new List<Task>();

            ImageLoader.ProcessImages(settings.SourcePath, settings.ResultPath, (image, filePath, targetFolder) =>
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                var thread = new Thread(() =>
                {
                    try
                    {
                        ProcessSingleImageAsync(imageHelper, settings, image, filePath, targetFolder);
                        taskCompletionSource.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                });
                thread.Start();
                tasks.Add(taskCompletionSource.Task);
            });

            await Task.WhenAll(tasks);
            return true;
        }

        private static void ProcessSingleImageAsync(ImageHelper imageHelper, HandlerSettings settings, Image image, string filePath, string targetFolder)
        {
            using (var newImage = imageHelper.BitmapFromPath(filePath))
            {
                if (settings.IsTransparent)
                    newImage.MakeTransparent(Color.Transparent);

                Bitmap bitmap = newImage;
                if (settings.RemoveBackground)
                {
                    imageHelper.RemovePixelsByColor(settings.Colors, newImage, settings.Tolerance);
                    Rectangle cropRectangle = imageHelper.FindLargestNonTransparentArea(newImage, settings.FindNonTransparent, settings.MinClusterSize);
                    Console.WriteLine($"Обработка: {filePath}");
                    bitmap = imageHelper.CropImage(newImage, cropRectangle);
                }

                if (settings.NeedResize)
                {
                    bitmap = imageHelper.ResizeWithSharpness(bitmap, settings.CropSizeX, settings.CropSizeY, settings.Interpolation);
                }

                var targetPath = GenerateTargetPath(filePath, targetFolder);
                CreateDirectoryIfNotExists(Path.GetDirectoryName(targetPath));

                Console.WriteLine($"Сохранено: {targetPath}");
                bitmap.Save(targetPath);

                // Освобождаем память и вызываем сборщик мусора
                bitmap.Dispose();
                bitmap = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }


        private static string GenerateTargetPath(string filePath, string targetFolder)
        {
            var subfolder = Path.GetDirectoryName(filePath).Substring(Path.GetDirectoryName(filePath).IndexOf(Path.DirectorySeparatorChar) + 1);
            return Path.Combine(targetFolder, Path.GetFileName(filePath));
        }

        static async Task Main(string[] args)
        {
            string jsonFilePath = Directory.GetCurrentDirectory() + "\\appsettings.json";
            string jsonText = File.ReadAllText(jsonFilePath);

            ImageProcessingOptions options = JsonConvert.DeserializeObject<ImageProcessingOptions>(jsonText);

            HandlerSettings settings = new HandlerSettings()
            {
                NeedResize = options.NeedResize,
                RemoveBackground = options.RemoveBackground,
                IsTransparent = options.IsTransparent,
                MinClusterSize = options.MinClusterSize,
                CropSizeX = options.CropSizeX,
                CropSizeY = options.CropSizeY,
                Tolerance = options.Tolerance,
                Interpolation = (InterpolationMode)Enum.Parse(typeof(InterpolationMode), options.Interpolation),
                FindNonTransparent = options.FindNonTransparent,
                SourcePath = options.SourcePath,
                ResultPath = options.ResultPath,
            };

            settings.Colors = GetColors(settings.InstanceImageHelper, options.Presets.FirstOrDefault(p => p.Name == options.CurrentPresetName));

            try
            {
                var result_state = await RunProcessImagesAsync(settings);
                Console.WriteLine("Выполнено");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка");
                Console.WriteLine(ex.Message);
            }
            await Console.Out.WriteLineAsync("\n\nНажмите на любую кнопку...");
            Console.ReadKey();
        }
    }
}
