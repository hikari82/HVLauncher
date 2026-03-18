using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
namespace HVLauncher
{
    public static class IconHelper
    {
        #region Native Interop
        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }
        [Flags]
        private enum SIIGBF
        {
            RESIZETOFIT = 0x00,
            BIGGERSIZEOK = 0x01,
            MEMORYONLY = 0x02,
            ICONONLY = 0x04,
            THUMBNAILONLY = 0x08,
            INCACHEONLY = 0x10
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(
                SIZE size,
                SIIGBF flags,
                out IntPtr phbm);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion

        private static readonly int[] IconSizes = { 16, 32, 48, 256 };

        public static ImageSource GetHighQualityIcon(string filePath, int size = 256)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            IShellItemImageFactory factory = null;
            IntPtr hBitmap = IntPtr.Zero;

            try
            {
                Guid iid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");

                SHCreateItemFromParsingName(
                    filePath,
                    IntPtr.Zero,
                    iid,
                    out factory);

                if (factory == null)
                    return null;

                SIZE sizeStruct = new SIZE { cx = size, cy = size };

                factory.GetImage(
                    sizeStruct,
                    SIIGBF.BIGGERSIZEOK | SIIGBF.ICONONLY,
                    out hBitmap);

                if (hBitmap == IntPtr.Zero)
                    return null;

                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze();

                return bitmapSource;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);

                if (factory != null)
                    Marshal.ReleaseComObject(factory);
            }
        }

        public static string SaveHighQualityIconToIco(string filePath, string outputFolder, string filename)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var source = GetHighQualityIcon(filePath, 256);
            if (source == null)
                return null;

            string outputPath = Path.Combine(outputFolder, filename + ".ico");

            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0);      // Reserved
                bw.Write((ushort)1);      // Type (1 = icon)
                bw.Write((ushort)IconSizes.Length); // Image count
                long directoryEntryStart = fs.Position;
                bw.Seek(IconSizes.Length * 16, SeekOrigin.Current);
                var imageDataList = new byte[IconSizes.Length][];
                var imageDataOffsets = new uint[IconSizes.Length];
                long imageDataOffset = 6 + (16 * IconSizes.Length);
                // Write image data for each size
                for (int i = 0; i < IconSizes.Length; i++)
                {
                    int size = IconSizes[i];
                    var resized = ResizeBitmap((BitmapSource)source, size);
                    var pngData = EncodePng(resized);

                    imageDataList[i] = pngData;
                    imageDataOffsets[i] = (uint)imageDataOffset;

                    bw.Seek((int)imageDataOffset, SeekOrigin.Begin);
                    bw.Write(pngData);

                    imageDataOffset += pngData.Length;
                }
                // Write directory entries
                bw.Seek((int)directoryEntryStart, SeekOrigin.Begin);

                for (int i = 0; i < IconSizes.Length; i++)
                {
                    int size = IconSizes[i];
                    byte width = (byte)(size == 256 ? 0 : size);
                    byte height = (byte)(size == 256 ? 0 : size);

                    bw.Write(width);                 // Width
                    bw.Write(height);                // Height
                    bw.Write((byte)0);              // Color count
                    bw.Write((byte)0);              // Reserved
                    bw.Write((ushort)1);            // Planes
                    bw.Write((ushort)32);           // Bit count
                    bw.Write(imageDataList[i].Length);  // Bytes in image
                    bw.Write(imageDataOffsets[i]);       // Offset of image data
                }
            }
            return outputPath;
        }

        private static BitmapSource ResizeBitmap(BitmapSource source, int size)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            int maxDimension = Math.Max(source.PixelWidth, source.PixelHeight);
            if (maxDimension == 0)
                return source;  // Can't scale if no size
            double scale = size / (double)maxDimension;
            // If scale is 1, no need to transform
            if (Math.Abs(scale - 1.0) < 0.0001)
                return source;
            var transform = new System.Windows.Media.ScaleTransform(scale, scale);
            var transformed = new TransformedBitmap(source, transform);
            var formatted = new FormatConvertedBitmap();
            formatted.BeginInit();
            formatted.Source = transformed;
            formatted.DestinationFormat = PixelFormats.Pbgra32;
            formatted.EndInit();
            formatted.Freeze();
            return formatted;
        }

        // Helper: Encode BitmapSource to PNG byte array
        private static byte[] EncodePng(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
    }
}