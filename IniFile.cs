using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HVLauncher
{
    public class IniFile
    {
        public string Path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(
            string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(
            string section, string key, string def,
            StringBuilder retVal, int size, string filePath);

        public IniFile(string path)
        {
            Path = path;

            if (!File.Exists(Path))
            {
                File.Create(Path).Close();
                IniFile ini = new IniFile(Path);
                ini.Write("Setting","RestoreSecurity","False");

            }
        }

        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, Path);
        }

        public string Read(string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", sb, 255, Path);
            return sb.ToString();
        }
    }
}
