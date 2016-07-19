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
                    | "Patch Available" | "Review" -> "Orange"
                    | "Declined" -> "Red"
                    | "Closed" | "Resolved" -> "Green"
                    | "In Progress" -> "DimGray"
                    | _ -> "Black"
            sprintf "<span style='color:%s'>%s</span>" color status

        let renderIssue (issue : JiraIssue) = 
            match issue.Parent with
                | Some(_) -> "&nbsp;&nbsp;○ "
                | _ -> ""
            + makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + formatStatus issue.Status

        let getWaitTime (issue : JiraIssue) = 
            sprintf "%s (%i days)" issue.Assignee (int (DateTime.Now - issue.Updated).TotalDays)

        let renderPatch (issue : JiraIssue) = 
            makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + getWaitTime issue

        let getSort (issue : JiraIssue) =
            match issue.Parent with
                | Some(parent) -> parent.Key
                | _ -> ""
            + issue.Key

        // TODO: Combobox with names, store last one in cookie?
        let renderItem (item : ReportItem) = 
            // Include parent issues in main list if missing to display subtasks properly
            let taskMissing t = 
                item.Tasks |> Seq.exists (fun x -> x.Key = t.Key) |> not

            let parents = item.Tasks 
                            |> Seq.map (fun x -> x.Parent) 
                            |> Seq.choose id
                            |> Seq.filter taskMissing
                            |> Seq.distinct

            let tasks = parents |> Seq.append item.Tasks |> Seq.sortBy getSort |> Seq.map renderIssue |> Seq.reduce concat
            let patches = 
                if item.Patches.Length > 0 
                    then item.Patches |> Seq.map renderPatch |> Seq.fold concat "<br/><br/><b>Pending Patches</b>" 
                    else ""
            sprintf "<h2>%s</h2>%s%s" item.Person tasks patches

        let report = items |> Seq.map renderItem |> Seq.reduce concat2
        sprintf "<h1>%s</h1>%s" (Jira.getTitle()) report

