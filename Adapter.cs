using System;
using System.IO;
using System.Management;

namespace HVLauncher
{
    public class Adapter
    {
        public static void Change(string[] args)
        {
            bool p = false;
            if (args.Length > 0)
                bool.TryParse(args[0], out p);

            RunAdapterLogic(p);
        }

        public static void LogMessage(string message)
        {
            string logFile = "app.log";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = String.Format("{0} - {1}", timestamp, message);
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        static void RunAdapterLogic(bool p)
        {
            string en = p ? "Enable" : "Disable";

            try
            {
                var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter"
                );
                foreach (ManagementObject adapter in searcher.Get())
                {
                    try
                    {
                        var typeObj = adapter["AdapterTypeID"];
                        int typeId = typeObj != null ? Convert.ToInt32(typeObj) : -1;
                        if (typeId == 11)
                        {
                            LogMessage("Skipping Bluetooth adapter: " + adapter["Name"]);
                            continue;
                        }
                        bool? netEnabled = adapter["NetEnabled"] as bool?;
                        if (netEnabled.HasValue && netEnabled.Value && !p)
                        {
                            LogMessage(en + " adapter: " + adapter["Name"]);
                            //adapter.InvokeMethod(en, null);
                        }
                        else if ((!netEnabled.HasValue || !netEnabled.Value) && p)
                        {
                            LogMessage(en + " adapter: " + adapter["Name"]);
                            //adapter.InvokeMethod(en, null);
                        }
                        else
                        {
                            LogMessage("Adapter already in desired state: " + adapter["Name"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Failed to " + en.ToLower() + " adapter: " + adapter["Name"] + " -> " + ex.Message);
                    }
                }

                LogMessage("Operation complete.");
            }
            catch (Exception ex)
            {
                LogMessage("Error querying adapters: " + ex.Message);
            }
        }
    }
}