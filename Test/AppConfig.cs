using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class PresetColor
    {
        public string Name { get; set; }
        public List<string> Colors { get; set; }
    }

    public class ImageProcessingOptions
    {
        public bool NeedResize { get; set; }
        public bool RemoveBackground { get; set; }
        public bool IsTransparent { get; set; }
        public int MinClusterSize { get; set; }
        public int CropSizeX { get; set; }
        public int CropSizeY { get; set; }
        public double Tolerance { get; set; }
        public string Interpolation { get; set; }
        public int FindNonTransparent { get; set; }
        public string SourcePath { get; set; }
        public string ResultPath { get; set; }
        public string CurrentPresetName { get; set; }
        public List<PresetColor> Presets { get; set; }
    }
}
