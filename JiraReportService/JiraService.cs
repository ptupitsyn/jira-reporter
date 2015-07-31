using System.Net;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace JiraReportService
{
    public class JiraService : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            Task.Run(() => ListenerThread());
        }

        protected override void OnStop()
        {
        }

        public static void ListenerThread()
        {
            var jira = new JiraReportGenerator();

            HttpServer.Run(r => GetResponse(r, jira), "http://*:3443/jiraReport/");
        }

        private static string GetResponse(HttpListenerRequest request, JiraReportGenerator jira)
        {
            return request.Url.ToString().ToLower().Contains("json") ? jira.ReportJson : jira.ReportHtml;
        }
    }
}