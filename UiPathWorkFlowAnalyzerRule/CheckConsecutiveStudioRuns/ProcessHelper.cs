using System.Diagnostics;
using System.Management;

namespace CheckConsecutiveStudioRuns
{
    internal class ProcessHelper
    {
        private static WindowWrapper _parentHwnd = null;

        public static WindowWrapper GetParentWindow()
        {
            if (_parentHwnd == null)
            {
                var currentProcessId = Process.GetCurrentProcess().Id;
                var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", currentProcessId);
                var search = new ManagementObjectSearcher("root\\CIMV2", query);
                var results = search.Get().GetEnumerator();
                results.MoveNext();
                var queryObj = results.Current;
                var parentId = (uint)queryObj["ParentProcessId"];
                var parentProcess = Process.GetProcessById((int)parentId);

                _parentHwnd = new WindowWrapper(parentProcess.MainWindowHandle);
            }

            return _parentHwnd;
        }
    }
}
