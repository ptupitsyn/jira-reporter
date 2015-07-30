using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace JiraReportService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Debugger.IsAttached)
                JiraService.ListenerThread();
            else
                ServiceBase.Run(new ServiceBase[] {new JiraService()});
        }
    }
}
