namespace JiraFsharpService
open System

module HtmlFormatter = 
    let renderReport (items : ReportItem[]) = 
        let concat acc x = acc + "<br />" + x
        let concat2 acc x = acc + "<br /><br />" + x
        let makeHeader x = "<h3>" + x + "</h3>"
        let makeLink text url = sprintf "<a href='%s'>%s</a>" url text

        let formatStatus status = 
            let color = 
                match status with
                    | "Patch Available" -> "Orange"
                    | "Closed" -> "Green"
                    | "In Progress" -> "DimGray"
                    | _ -> "Black"
            sprintf "<span style='color:%s'>%s</span>" color status        


        let renderIssue (issue : JiraIssue) = 
            makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + formatStatus issue.Status

        let getWaitTime (issue : JiraIssue) = 
            sprintf "%s (waiting %i days)" issue.Assignee (int (DateTime.Now - issue.Updated).TotalDays)

        let renderPatch (issue : JiraIssue) = 
            makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + getWaitTime issue

        let renderItem (item : ReportItem) = 
            sprintf "%s<br/>%s<br/><b>Pending Patches</b><br/>%s" item.Person 
                (item.Tasks |> Seq.map renderIssue |> Seq.reduce concat)
                (item.Patches |> Seq.map renderPatch |> Seq.reduce concat)

        let title = makeHeader (Jira.getTitle())
        let report = items |> Seq.map renderItem |> Seq.reduce concat2
        sprintf "%s<br/>%s" title report

