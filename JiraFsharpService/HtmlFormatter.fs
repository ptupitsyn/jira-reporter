namespace JiraFsharpService
open System

module HtmlFormatter = 
    let renderReport (items : seq<ReportItem>) = 
        let concat acc x = acc + "<br />" + x
        let concat2 acc x = acc + "<br /><br /><hr />" + x
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
            sprintf "%s (%i days)" issue.Assignee (int (DateTime.Now - issue.Updated).TotalDays)

        let renderPatch (issue : JiraIssue) = 
            makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + getWaitTime issue

        // TODO: Combobox with names, store last one in cookie?
        let renderItem (item : ReportItem) = 
            let tasks = item.Tasks |> Seq.map renderIssue |> Seq.reduce concat
            let patches = 
                if item.Patches.Length > 0 
                    then item.Patches |> Seq.map renderPatch |> Seq.fold concat "<br/><br/><b>Pending Patches</b>" 
                    else ""
            sprintf "<h2>%s</h2>%s%s" item.Person tasks patches

        let report = items |> Seq.map renderItem |> Seq.reduce concat2
        sprintf "<h1>%s</h1>%s" (Jira.getTitle()) report

