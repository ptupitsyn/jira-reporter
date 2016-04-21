using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace JiraReportService
{
    public class JiraReportGenerator
    {
        private string _reportHtml = "";

        public JiraReportGenerator()
        {
            Status = "Starting...";
            ReportJson = "";
            Task.Run(() => UpdaterThread());
        }

        public string ReportHtml
        {
            get { return _reportHtml + Status; }
            private set { _reportHtml = value; }
        }

        public string ReportJson { get; private set; }

        public string Status { get; private set; }

        private void UpdaterThread()
        {
            while (true)
            {
                try
                {
                    UpdateReport();
                    Status = "Last updated: " + DateTime.Now;
                }
                catch (Exception ex)
                {
                    Status = "Error: " + ex;
                }

                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private IEnumerable<Issue> GetIssues(Jira jira)
        {
            Status = "Processing JQL...";

            var jql = "updated>-12h%20AND%20status%20not%20in%20(open)";

            if (!string.IsNullOrEmpty(jira.Project))
                jql = string.Format("project%3D{0}%20AND%20{1}", jira.Project, jql);

            dynamic dyn = QueryApi(string.Format("search?jql={0}&maxResults=1000", jql), jira);

            return GetIssues(dyn, jira);
        }


        private void UpdateReport()
        {
            var issues = Jira.Instances.AsParallel().SelectMany(GetIssues).ToArray();

            var dt = DateTime.Now;

            var authors = issues.SelectMany(x => x.History).Select(x => x.Author).Distinct().OrderBy(x => x);

            var report = authors.Select(author => new ReportItem
            {
                Author = author,
                Issues = issues.Where(x =>
                    x.History.Any(h => h.Author == author && h.Date > dt.AddHours(-12))
                    || x.Comments.Any(h => h.Author == author && h.Date > dt.AddHours(-12))
                    ).ToArray()
            }).Where(x => x.Issues.Any()).ToArray();

            ReportHtml = GetReportHtml(report);
            ReportJson = GetReportJson(report);
        }

        private static string GetReportHtml(ReportItem[] reportItems)
        {
            var dt = DateTime.Now;
            var sb = new StringBuilder();
            sb.AppendFormat("<h1>DAILY STATUS ({0}/{1}/{2})</h1>", dt.Month, dt.Day, dt.Year);


            foreach (var item in reportItems)
            {
                sb.AppendFormat("<br/><h3>{0}</h3>", item.Author);

                var html = item.Issues.Select(FormatIssue).Aggregate((x, y) => x + "<br />" + y);

                sb.Append(html);
            }

            sb.Append("<br/><p/><hr/><br/>");
            sb.Append(
                "<p><a href='https://github.com/kefir0/jira-reporter/'>https://github.com/kefir0/jira-reporter/</a></p>");

            var reportHtml = sb.ToString();
            return reportHtml;
        }

        private static string GetReportJson(ReportItem[] reportItems)
        {
            return Json.Encode(reportItems);
        }

        private static string FormatIssue(Issue x)
        {
            return string.Format(
                "<a href='{3}/browse/{0}'>{0} {1}</a> - {2}", x.Key,
                x.Summary.Trim('.'), x.Status, x.JiraUrl);
        }

        private static dynamic QueryApi(string query, Jira jira)
        {
            var wc = jira.GetAuthorizedWebClient();
            
            var resp = wc.DownloadString(jira.Url + "/rest/api/latest/" + query);

            return Json.Decode(resp);
        }

        private IEnumerable<Issue> GetIssues(dynamic response, Jira jira)
        {
            int total = response.total;
            var cur = 0;

            foreach (var i in response.issues)
            {
                Status = string.Format("Loading issue {0} of {1}", cur++, total);

                var data = QueryApi(string.Format("issue/{0}?expand=changelog", i.Key), jira);

                yield return new Issue
                {
                    Key = i.Key,
                    Summary = i.fields.summary,
                    Status = i.fields.status.name,
                    Comments = Enumerable.ToArray(GetItems(data.fields.comment.comments)),
                    History = Enumerable.ToArray(GetItems(data.changelog.histories)),
                    JiraUrl = jira.Url
                };
            }
        }

        private static IEnumerable<Item> GetItems(dynamic obj)
        {
            foreach (var i in obj)
            {
                yield return new Item
                {
                    Author = i.author.displayName,
                    Date = DateTime.Parse(i.updated ?? i.created),
                    Content = i.body
                };
            }
        }
    }

    public class Issue
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Status { get; set; }
        public Item[] Comments { get; set; }
        public Item[] History { get; set; }
        public string JiraUrl { get; set; }
    }

    public class Item
    {
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Content { get; set; }
    }

    public class ReportItem
    {
        public string Author { get; set; }
        public Issue[] Issues { get; set; }
    }
}