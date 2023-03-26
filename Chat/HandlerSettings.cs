using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    public class HandlerSettings
    {
        private static readonly ImageHelper _helper = new ImageHelper();
        public ImageHelper InstanceImageHelper { get { return _helper; } }
        public string SourcePath { get; set; }
        public string ResultPath { get; set; }
        public bool NeedResize { get; set; }
        public bool RemoveBackground { get; set; }
        public int CropSizeX { get; set; }
        public int CropSizeY { get; set; }
        public double Tolerance { get; set; }
        public int FindNonTransparent { get; set; }
        public List<Color> Colors { get; set; }
        public bool IsTransparent { get; set; }
        public HandlerSettings()
        {
            Colors = new List<Color>();
        }
    }
}
