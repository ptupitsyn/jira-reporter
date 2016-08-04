using System;
using System.IO;
using System.Net;
using System.Text;

namespace JiraReportService
{
    public class Jira
    {
        public static Jira[] Instances =
        {
            new Jira("https://issues.apache.org/jira", "IGNITE", null),
            new Jira("http://atlassian.gridgain.com/jira", null, File.ReadAllLines("d:\\jira.jira"))
        };

        private readonly string[] _creds;
        private readonly string _project;
        private readonly string _url;

        private Jira(string url, string project, string[] creds)
        {
            _url = url;
            _project = project;
            _creds = creds;
        }

        public string Url
        {
            get { return _url; }
        }

        public string Project
        {
            get { return _project; }
        }

        public WebClient GetAuthorizedWebClient()
        {
            var webClient = new WebClient();

            if (_creds == null)
                return webClient;

            webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + GetEncodedCredentials();
            return webClient;
        }

        private string GetEncodedCredentials()
        {
            // ReSharper disable once CoVariantArrayConversion
            var mergedCredentials = string.Format("{0}:{1}", _creds);
            var byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }
    }
}