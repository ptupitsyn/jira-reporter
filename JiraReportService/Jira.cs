 using System;
 using System.IO;
 using System.Net;
using System.Text;

namespace JiraReportService
{
    public class Jira
    {
        public static Jira[] Instances = {
            new Jira("https://issues.apache.org/jira", "IGNITE"),
            new Jira("http://atlassian.gridgain.com/jira", null)
        };

        private static readonly string[] Creds = File.ReadAllLines("d:\\jira.jira");

        private readonly string _url;

        private readonly string _project;

        private Jira(string url, string project)
        {
            _url = url;
            _project = project;
        }

        public string Url
        {
            get { return _url; }
        }

        public string Project
        {
            get { return _project; }
        }

        private static string GetEncodedCredentials()
        {
            // ReSharper disable once CoVariantArrayConversion
            var mergedCredentials = string.Format("{0}:{1}", Creds);
            var byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }

        public WebClient GetAuthorizedWebClient()
        {
            var webClient = new WebClient();

            if (!_url.Contains("gridgain"))
                return webClient;

            webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + GetEncodedCredentials();
            return webClient;
        }
    }
}