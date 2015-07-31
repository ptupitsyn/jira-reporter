using System.Diagnostics;
using System.ServiceProcess;

namespace JiraReportService
{
    internal static class Program
    {
        private static void Main()
        {
            if (Debugger.IsAttached)
                JiraService.ListenerThread();
            else
                ServiceBase.Run(new ServiceBase[] {new JiraService()});
        }
    }
}