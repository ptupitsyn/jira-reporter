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
        private string _reportJson = "";
        private string _status = "Starting...";

        public JiraReportGenerator()
        {
            ReportHtml = ReportJson = "Loading...";
            Task.Run(() => UpdaterThread());
        }

        public string ReportHtml
        {
            get { return _reportHtml + _status; }
            private set { _reportHtml = value; }
        }

        public string ReportJson
        {
            get { return _reportJson; }
            private set { _reportJson = value; }
        }

        public string Status
        {
            get { return _status; }
            private set { _status = value; }
        }

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

                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }

        private IEnumerable<ReportItem> GetReport()
        {
            Status = "Processing JQL...";

            dynamic dyn = QueryApi("search?jql=updated%20%3E%20%22-12h%22&maxResults=1000");

            List<Issue> issues = Enumerable.ToList(GetIssues(dyn));

            var dt = DateTime.Now;

            var authors = issues.SelectMany(x => x.History).Select(x => x.Author).Distinct().OrderBy(x => x);

            return authors.Select(author => new ReportItem
            {
                Author = author,
                Issues = issues.Where(x => x.History.Any(h => h.Author == author && h.Date > dt.AddHours(-12)))
                    .ToArray()
            }).Where(x => x.Issues.Any());
        }


        private void UpdateReport()
        {
            var reportItems = GetReport().ToArray();

            ReportHtml = GetReportHtml(reportItems);
            ReportJson = GetReportJson(reportItems);
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
                "<a href='http://atlassian.gridgain.com/jira/browse/{0}'>{0} {1}</a> - {2}", x.Key,
                x.Summary.Trim('.'), x.Status);
        }

        private static dynamic QueryApi(string query)
        {
            var wc = Jira.GetAuthorizedWebClient();
            var resp = wc.DownloadString("http://atlassian.gridgain.com/jira/rest/api/latest/" + query);

            return Json.Decode(resp);
        }

        private IEnumerable<Issue> GetIssues(dynamic response)
        {
            int total = response.total;
            int cur = 0;

            foreach (var i in response.issues)
            {
                Status = string.Format("Loading issue {0} of {1}", cur++, total);

                if (i.fields.status.name != "Open")
                {
                    var data = QueryApi(string.Format("issue/{0}?expand=changelog", i.Key));
                    //Console.WriteLine(data);
                    yield return new Issue
                    {
                        Key = i.Key,
                        Summary = i.fields.summary,
                        Status = i.fields.status.name,
                        Comments = Enumerable.ToArray(GetItems(data.fields.comment.comments)),
                        History = Enumerable.ToArray(GetItems(data.changelog.histories))
                    };
                }
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