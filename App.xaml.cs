using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Threading;

namespace HVLauncher
{
    public partial class App : Application
    {
        private bool debug = false;

        private string csvFilePath = AppDomain.CurrentDomain.BaseDirectory + "/gamelist.csv";
        private List<CsvRow> rows = new List<CsvRow>();
        IniFile ini = new IniFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ini"));

        private void RunCommand(string[] c)
        {
            CommandHelper.RunCmd(c, true, true);
        }

        public static void LogMessage(string message)
        {
            string logFile = "app.log";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = String.Format("{0} - {1}", timestamp, message);
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        private void Kill(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.ToLower().StartsWith(name))
                {
                    clsProcess.Kill();
                }
            }
        }

        private void getDriversAndDelete()
        {
            string sn = "", sn2 ="";
            try
            {
                sn = GetServiceNameByDriver("hyperkd.sys");
                sn2 = GetServiceNameByDriver("simplesvm.sys");
                if (sn.Trim().Length > 0)
                {
                    RunCommand(new string[] 
                { 
                    "sc stop " + sn,
                    "sc config "+sn+" start= disabled",
                    "sc delete " + sn
                });
                }
                if (sn2.Trim().Length > 0)
                {
                    RunCommand(new string[] 
                { 
                    "sc stop " + sn2,
                    "sc config "+sn2+" start= disabled",
                    "sc delete " + sn2
                });
                }
            }
            catch (Exception e)
            {
                LogMessage(e.Message.ToString());
            }
        }

        public string GetServiceNameByDriver(string driverFile)
        {
            string r = "";
            var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true);

            foreach (var serviceName in servicesKey.GetSubKeyNames())
            {
                var serviceKey = servicesKey.OpenSubKey(serviceName);

                var imagePath = serviceKey.GetValue("ImagePath") as string;

                if (imagePath != null &&
                    imagePath.IndexOf(driverFile, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    r = serviceName;
                }
            }
            return r;
        }

        private void StartGameAndWait(string path)
        {
            try
            {
                ProcessStartInfo psiGame = new ProcessStartInfo(path);
                psiGame.WorkingDirectory = Path.GetDirectoryName(path);
                using (Process proc = Process.Start(psiGame))
                {
                    proc.WaitForExit();
                }
            }
            catch (Exception e)
            {
                LogMessage(e.Message.ToString());
            }
        }
        
        private bool restartMSI()
        {
            bool r = false;
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.ToLower().StartsWith("msiafterburner"))
                {
                    clsProcess.Kill();
                    r = true;
                }
            }
            return r;
        }
        
        private void startMSI()
        {
            string pname = "C:\\Program Files (x86)\\MSI Afterburner\\MSIAfterburner.exe";
            CommandHelper.RunCmd(new string[] { "\"" + pname + "\"" }, true, false);
        }

        private void runDSE(string p, string i)
        {
            string command = "\"" + p +"\" -"+i;
            RunCommand(new string[]{command});
        }

        private string extractDSE()
        {
            string resourceName = "HVLauncher.DSE-Patcher.exe";
            string outputPath = Path.Combine(Path.GetTempPath(), "DSE.exe");

            using (Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceName))
            using (FileStream file = new FileStream(outputPath, FileMode.Create))
            {
                stream.CopyTo(file);
            }
            return outputPath;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //Adapter.Change(new string[]{"Disable"});
            bool isMSI = false;
            getDriversAndDelete();
            if (e.Args.Length > 0)
            {
                string firstArg = e.Args[0];
                CsvRow found = CsvHelper.ReadCsv(csvFilePath).FirstOrDefault(
                    r => r.filename != null &&
                         r.filename.Equals(firstArg, StringComparison.OrdinalIgnoreCase)
                );
                if (found == null)
                {
                    MessageBox.Show("Error, Game is deleted from the List, please recreate shortcut");
                    Environment.Exit(0);
                }
                if (found != null)
                {
                    if (!System.IO.File.Exists(found.GamePath))
                    {
                        MessageBox.Show("Game exe file not found, launcher will exit");
                        Environment.Exit(0);

                    }
                    if (!System.IO.File.Exists(found.HVPath))
                    {
                        MessageBox.Show("Hypervisor files not found, launcher will exit");
                        Environment.Exit(0);
                    }
                }
                if ( (System.IO.File.Exists(found.GamePath)) && (System.IO.File.Exists(found.HVPath)))
                {
                    string hvname = "HV" + found.filename.Replace(" ", string.Empty).Replace(".exe", string.Empty);
                    if (found.GameName.Trim().Length>0)
                    {
                       hvname = "HV" + found.GameName.Replace(" ", string.Empty);
                    }
                    string createdenuvo = "";

                    if (!debug)
                    {
                        createdenuvo = "sc create " + hvname + " type= kernel start= demand binPath= \"" + found.HVPath + "\"";
                    }
                    else
                    {
                        createdenuvo = "sc create " + hvname + " type=own start=demand binPath=" + found.HVPath;
                    }
                        
                    isMSI = restartMSI();
                    Thread.Sleep(100);
                    string dse = extractDSE();
                    if (!debug)
                    {
                        Thread.Sleep(100);
                        runDSE(dse, "disable");
                    }
                    
                    if (found.LaunchService)
                    {
                        if (!debug)
                        {
                            Thread.Sleep(100);
                            RunCommand(new string[] { createdenuvo, "sc start " + hvname });
                            Thread.Sleep(500);
                            runDSE(dse, "enable");
                            Thread.Sleep(100);
                            if (isMSI) startMSI();
                            Thread.Sleep(500);
                            StartGameAndWait(found.GamePath);
                            Thread.Sleep(500);
                            RunCommand(new string[] { "sc stop " + hvname, "sc delete " + hvname });
                        }
                    }
                    else
                    {
                        if (!debug)
                        {
                            Thread.Sleep(100);
                            if (isMSI) startMSI();
                            Thread.Sleep(500);
                            StartGameAndWait(found.GamePath);
                            Thread.Sleep(500);
                            runDSE(dse, "enable");
                        }
                    }
                    Thread.Sleep(100);
                    isMSI = restartMSI();
                    Thread.Sleep(100);
                    if (isMSI) startMSI();
                    Thread.Sleep(100);
                    getDriversAndDelete();
                    Thread.Sleep(100);
                    
                    if(bool.Parse(ini.Read("Setting","RestoreSecurity")))
                    {
                         CommandHelper.RunCmd(new string[] { "bcdedit /set hypervisorlaunchtype Auto"
                        ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f"
                        ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 1 /f"
                        ,"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v LsaCfgFlags /t REG_DWORD /d 1 /f"
                        }, true, true);
                    }
                    Thread.Sleep(200);
                    Environment.Exit(0);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                ;
            }
            base.OnStartup(e);
        }
    }
}
