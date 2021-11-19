using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CheckConsecutiveStudioRuns
{
    class RobotFactory
    {
        private static string RobotExeName = "UiRobot.exe";
        private static string RobotPath="";

        static RobotFactory()
        {
            string robotExePath = Path.GetDirectoryName(GetProcessInformations());
            if (!string.IsNullOrEmpty(robotExePath))
            {
                string path = Path.Combine(robotExePath, RobotExeName);
                RobotPath = path;
            }           
            
        }
        static string GetProcessInformations()
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_Process");
            var processList = searcher.Get();
            string studioPath = "";
            foreach (var process in processList)
            {
                var processName = process["Name"];
                var processPath = process["ExecutablePath"];

                if (processName.ToString() == "UiPath.Studio.exe")
                {
                    studioPath = processPath.ToString();
                }
            }
            return studioPath;
        }
        public void RunAutomation(string loggingProcess, string projectName, string mainXamlHash)
        {
            if ((!string.IsNullOrEmpty(loggingProcess) || string.IsNullOrEmpty(RobotPath)))
            {                
                string processArguement = string.Format("--input \"{{'processName': '{0}', 'HashKey': '{1}'}}\"", projectName, mainXamlHash);
                Process robotProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = RobotPath;
                startInfo.Arguments = string.Format("execute --process {0} {1}", loggingProcess, processArguement);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                robotProcess.StartInfo = startInfo;
                robotProcess.EnableRaisingEvents = true;
                robotProcess.Start();
                robotProcess.WaitForExit();
            }
            
        }
    }
}
