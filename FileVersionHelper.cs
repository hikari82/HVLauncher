using System;
using System.IO;
using System.Runtime.InteropServices;
namespace HVLauncher
{
    public static class FileVersionHelper
    {
        [DllImport("version.dll", CharSet = CharSet.Auto)]
        private static extern bool GetFileVersionInfo(
            string lptstrFilename,
            int dwHandle,
            int dwLen,
            byte[] lpData);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        private static extern int GetFileVersionInfoSize(string lptstrFilename, out int lpdwHandle);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        private static extern bool VerQueryValue(
            byte[] pBlock,
            string lpSubBlock,
            out IntPtr lplpBuffer,
            out uint puLen);

        public static string GetProductName(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            int handle;
            int size = GetFileVersionInfoSize(filePath, out handle);
            if (size == 0)
                return null;
            byte[] buffer = new byte[size];
            if (!GetFileVersionInfo(filePath, 0, size, buffer))
                return null;
            IntPtr ptr;
            uint len;
            if (VerQueryValue(buffer, @"\StringFileInfo\040904B0\ProductName", out ptr, out len) && len > 0)
            {
                return Marshal.PtrToStringUni(ptr);
            }
            return null;
        }
        public static string GetFileVersion(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            int handle;
            int size = GetFileVersionInfoSize(filePath, out handle);
            if (size == 0)
                return null;
            byte[] buffer = new byte[size];
            if (!GetFileVersionInfo(filePath, 0, size, buffer))
                return null;
            IntPtr ptr;
            uint len;
            if (VerQueryValue(buffer, @"\StringFileInfo\040904B0\ProductVersion", out ptr, out len) && len > 0)
            {
                return Marshal.PtrToStringUni(ptr);
            }

            return null;
        }
    }
}
