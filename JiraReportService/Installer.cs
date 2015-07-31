using System.ComponentModel;
using System.ServiceProcess;

namespace JiraReportService
{
    [RunInstaller(true)]
    public class Installer : System.Configuration.Install.Installer
    {
        //private ServiceInstaller serviceInstaller;
        //private ServiceProcessInstaller processInstaller;

        public Installer()
        {
            // Instantiate installers for process and services.
            var processInstaller = new ServiceProcessInstaller {Account = ServiceAccount.LocalSystem};

            // The services run under the system account.
            var serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Manual,
                ServiceName = "JiraService"     // ServiceName must equal those on ServiceBase derived classes.
            };

            // Add installers to collection. Order is not important.
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}