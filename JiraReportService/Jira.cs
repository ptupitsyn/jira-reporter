using System;
using System.Net;
using System.Text;

namespace JiraReportService
{
    public static class Jira
    {
        private static string GetEncodedCredentials()
        {
            // TODO
            var mergedCredentials = string.Format("{0}:{1}", "", "");
            var byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }

        public static WebClient GetAuthorizedWebClient()
        {
            var webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + GetEncodedCredentials();
            return webClient;
        }
    }
}