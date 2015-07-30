 using System;
 using System.IO;
 using System.Net;
using System.Text;

namespace JiraReportService
{
    public static class Jira
    {
        private static readonly string[] Creds = File.ReadAllLines("d:\\jira.jira");

        private static string GetEncodedCredentials()
        {
            // ReSharper disable once CoVariantArrayConversion
            var mergedCredentials = string.Format("{0}:{1}", Creds);
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