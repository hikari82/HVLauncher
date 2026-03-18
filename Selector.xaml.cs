using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using System.IO;
namespace HVLauncher
{
    public partial class Selector : Window
    {
        private string csvFilePath = AppDomain.CurrentDomain.BaseDirectory + "/gamelist.csv";
        private List<CsvRow> rows = new List<CsvRow>();
        IniFile ini = new IniFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ini"));

        public Selector ()
        {
            InitializeComponent();
            BtnAdd.Click += BtnAdd_Click;
            BtnDelete.Click += BtnDelete_Click;
            BtnCreate.Click += BtnCreate_Click;
            RefreshGrid();
            lblvbs.Content = VBSStatus();
            lblcorei.Content = CIStatus();
            lblhvtype.Content = HVStatus();
            cbRevert.IsChecked = bool.Parse(ini.Read("Setting", "RestoreSecurity"));
        }

        private string VBSStatus()
        {
            string r = "";
            r = CommandHelper.RunCmdOutput(new string[] { "wmic /namespace:\\\\root\\Microsoft\\Windows\\DeviceGuard path Win32_DeviceGuard get VirtualizationBasedSecurityStatus" }, true).ToLower();
            if (r.Contains("0"))
            {r = "Not Enabled";}
            else
            if (r.Contains("1"))
            {r = "Inactive";}
            else
            if (r.Contains("2"))
            { r = "Active"; }
            else
                r = "Unknown";
            return "VBS : " + r;
        }

        private string CIStatus()
        {
            string r = "";
            r = CommandHelper.RunCmdOutput(new string[] { "wmic /namespace:\\\\root\\Microsoft\\Windows\\DeviceGuard path Win32_DeviceGuard get SecurityServicesRunning" }, true).ToLower();
            if (r.Contains("0"))
            {r = "Disabled";}
            else
            if (r.Contains("1"))
            {r = "Enabled";}
            else
                r = "Unknown";
            return "Core Isolation : " + r;
        }

        private string HVStatus()
        {
            string r = "";
            r = CommandHelper.RunCmdOutput(new string[] { "bcdedit /enum | findstr /i hypervisorlaunchtype" }, true).ToLower();
            if (r.ToLower().Contains("auto"))
            { r = "Auto"; }
            else
                if (r.ToLower().Contains("off"))
                { r = "Off"; }
                else
                    if (r.ToLower().Contains("boot"))
                    { r = "Boot"; }
                    else
                        r = "Unknown";
            return "Hypervisor Launch Type : " + r;
        }
        private void LaunchServiceChanged(object sender, RoutedEventArgs e)
        {
            CsvHelper.SaveCsv(csvFilePath, rows);
        }

        private void SaveCsv()
        {
            CsvHelper.SaveCsv(csvFilePath, rows);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string hvPath = SelectFile(2);
            string exePath = SelectFile(1);
            if (string.IsNullOrEmpty(exePath) || string.IsNullOrEmpty(hvPath)) return;
            string productName = FileVersionHelper.GetProductName(exePath);
            string fname = System.IO.Path.GetFileName(exePath);
            string iconFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon");
            if (!System.IO.Directory.Exists(iconFolder))
                System.IO.Directory.CreateDirectory(iconFolder);
            string iconPath = IconHelper.SaveHighQualityIconToIco(exePath, iconFolder, fname.Replace(" ", String.Empty).Replace(".exe", String.Empty));
            
            if (productName==null)
            {
                productName = fname.Replace(" ", String.Empty).Replace(".exe", String.Empty);
            }
            CsvRow newRow = new CsvRow
            {
                GameName = productName,
                IconPath = iconPath,
                GamePath = exePath,
                HVPath = hvPath,
                filename = fname,
                LaunchService = false
            };
            CsvHelper.AddRow(csvFilePath, newRow);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            rows = CsvHelper.ReadCsv(csvFilePath);
            DataGridCsv.ItemsSource = null;
            DataGridCsv.ItemsSource = rows;
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCsv.SelectedIndex < 0) 
            {
                MessageBox.Show("Select one game from the list first!");
                return;
            }
            CsvRow selectedRow = rows[DataGridCsv.SelectedIndex];
            CreateShortcut(selectedRow.GameName, selectedRow.IconPath, selectedRow.GamePath, selectedRow.HVPath, selectedRow.filename);
            MessageBox.Show("Shortcut created on Desktop!");
        }

        private void CreateShortcut(string a, string b, string c, string d, string e)
        {
            string exePath = c;
            string projectFolder = AppDomain.CurrentDomain.BaseDirectory;
            string projectExe = System.IO.Path.Combine(projectFolder, "HVLauncher.exe");
            string iconSource = b;
            string productName = FileVersionHelper.GetProductName(exePath) ?? System.IO.Path.GetFileNameWithoutExtension(exePath);
            string fileVersion = FileVersionHelper.GetFileVersion(exePath) ?? " v1.0.0.0";
            string shortcutName = productName + " v" + fileVersion;
            string myArgument = e;
            ShortcutHelper.CreateDesktopShortcut(projectExe, iconSource, shortcutName, myArgument, e.Replace(" ", String.Empty).Replace(".exe", String.Empty));
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCsv.SelectedIndex < 0)
            {
                MessageBox.Show("Select one game from the list first!");
                return;
            }
            CsvHelper.DeleteRow(csvFilePath, DataGridCsv.SelectedIndex);
            rows = CsvHelper.ReadCsv(csvFilePath);
            DataGridCsv.ItemsSource = null;
            DataGridCsv.ItemsSource = rows;
            RefreshGrid();
        }

        private string SelectFile(int i)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (i == 1)
            {
                dlg.Filter = "Game Executeable File (*.exe)|*.exe";
                dlg.Title = "Select Game Executeable File";
            }
            if (i == 2)
            {
                dlg.Filter = "Hypervisor Driver File (*.sys)|*.sys";
                dlg.Title = "Select Hypervisor Driver File";
            }
            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            return "";
        }

        private void cbRevert_Checked(object sender, RoutedEventArgs e)
        {
            string cb = cbRevert.IsChecked.ToString();
            ini.Write("Setting", "RestoreSecurity", cb);
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
            "This will Restart the PC, Proceed ?",
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                CommandHelper.RunCmd(new string[] { "bcdedit /set hypervisorlaunchtype off"
                ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f"
                ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 0 /f"
                ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v LsaCfgFlags /t REG_DWORD /d 0 /f"
                ,"shutdown /r /t 0"
                }, true, true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bool.Parse(ini.Read("Setting", "RestoreSecurity")))
            {
                MessageBoxResult result = MessageBox.Show(
               "You have checked Restore Security, This will revert VBS and Hypervisor setting on next Reboot, Proceed ?",
               "Confirm",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    CommandHelper.RunCmd(new string[] { "bcdedit /set hypervisorlaunchtype Auto"
                    ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f"
                    ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 1 /f"
                    ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v LsaCfgFlags /t REG_DWORD /d 1 /f"
                    //,"shutdown /r /t 0"
                }, true, true);
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

    }
}
