using IWshRuntimeLibrary;
using System;
using System.IO;
namespace HVLauncher
{
    public static class ShortcutHelper
    {
        public static void CreateDesktopShortcut(
            string targetExePath,
            string iconSourceExePath,
            string shortcutName,
            string argument,
            string productName)
        {
            if (!System.IO.File.Exists(targetExePath))
                throw new FileNotFoundException("Target EXE not found", targetExePath);
            if (!System.IO.File.Exists(iconSourceExePath))
                throw new FileNotFoundException("Icon source EXE not found", iconSourceExePath);
            string iconFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon");
            string iconPath = iconFolder + "\\" + productName + ".ico";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, shortcutName + ".lnk");
            var shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
            shortcut.IconLocation = iconPath;
            shortcut.Arguments = argument;
            shortcut.Save();
        }
    }
}
