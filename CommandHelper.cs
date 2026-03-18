using System;
using System.Diagnostics;
using System.IO;
namespace HVLauncher
{
    public static class CommandHelper
    {
        public static void RunCmd(string[] commands, bool elevated, bool waitForExit)
        {
            string combined = string.Join(" & ", commands);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c " + combined;

            if (elevated)
            {
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }
            
            string error = "";
            try
            {
                
                using (Process process = Process.Start(psi))
                {
                    try
                    {
                        error = process.StandardError.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        error = e.Message.ToString();
                    }
                    if (waitForExit)
                        process.WaitForExit();
                    if (error.Trim().Length > 0)
                    {
                        LogMessage(error);
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message.ToString();
                if (error.Trim().Length > 0)
                {
                    LogMessage(error);
                }
            }
        }

        public static void LogMessage(string message)
        {
            string logFile = "app.log";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = String.Format("{0} - {1}", timestamp, message);
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        public static string RunCmdOutput(string[] commands, bool waitForExit)
        {
            string combined = string.Join(" & ", commands);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c " + combined;
            psi.UseShellExecute = false;
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            string output = "";
            string error = "";
            try
            {
                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                        return null;

                    try
                    {
                        error = process.StandardError.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        error = e.Message.ToString();
                    }

                    try
                    {
                        output = process.StandardOutput.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        error += e.Message.ToString();
                    }

                    if (waitForExit)
                        process.WaitForExit();
                    if(error.Trim().Length>0)
                    {
                        LogMessage(error);
                    }
                    return output + error;
                }
            }
            catch
            {
                return "";
            }
        }

        public static void RunPowerShell(string[] commands, bool elevated, bool waitForExit)
        {
            string combined = string.Join(" ; ", commands);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + combined + "\"";

            if (elevated)
            {
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }

            try
            {
                Process process = Process.Start(psi);
                if (waitForExit && process != null)
                    process.WaitForExit();
            }
            catch
            {
                
            }
        }
    }
}
