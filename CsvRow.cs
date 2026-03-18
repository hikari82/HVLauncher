using System;
using System.Windows.Media.Imaging;
namespace HVLauncher
{
    public class  CsvRow
    {
        public string GameName { get; set; }
        public string IconPath { get; set; }
        public string GamePath { get; set; }
        public string HVPath { get; set; }
        public string filename { get; set; }
        public BitmapImage IconImage
        {
            get
            {
                if (System.IO.File.Exists(IconPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(IconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    return bitmap;
                }
                return null;
            }
        }
        public bool LaunchService { get; set; }
    }
}
