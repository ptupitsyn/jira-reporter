namespace JiraFsharpService
open System
open System.Web

module HtmlFormatter = 
    let renderReport (items : seq<ReportItem>) (showComments : bool) (personFilter : string) = 
        let concat acc x = acc + "<br />" + x
        let concat2 acc x = acc + "<br /><br /><hr />" + x
        let makeLink text url = sprintf "<a href='%s'>%s</a>" url text
        let urlEncode (s : string) = HttpUtility.UrlPathEncode(s)

        let formatStatus status = 
            let color = 
                match status with
                    | "Patch Available" | "Review" -> "Orange"
                    | "Declined" -> "Red"
                    | "Closed" | "Resolved" -> "Green"
                    | "In Progress" -> "DimGray"
                    | _ -> "Black"
            let eta = if status = "In Progress" then " (ETA: 1d)" else ""
            sprintf "<span style='color:%s'>%s%s</span>" color status eta

        let renderComment (issue : JiraIssue) = 
            match issue.Comment with
                | Some(x) -> sprintf "<br />&nbsp;&nbsp;&nbsp;&nbsp;<i>%s: %s</i>" x.Author x.Body
                | _ -> ""

        let renderIssue (issue : JiraIssue) = 
            match issue.Parent with
                | Some(_) -> "&nbsp;&nbsp;○ "
                | _ -> ""
            + makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + formatStatus issue.Status 
            + if showComments then renderComment issue else ""
            + "<br />"

        let getWaitTime (issue : JiraIssue) = 
            sprintf "%s (%i days)" issue.Assignee (int (DateTime.Now - issue.Updated).TotalDays)

        let renderPatch (issue : JiraIssue) = 
            makeLink (issue.Key + " " + issue.Summary) issue.Url + " - " + getWaitTime issue

        let getSort (issue : JiraIssue) =
            match issue.Parent with
                | Some(parent) -> parent.Key
                | _ -> ""
            + issue.Key

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

        let filteredItems = 
            if personFilter = "" 
                then items 
                else items |> Seq.where (fun x -> x.Person.IndexOf(personFilter, StringComparison.OrdinalIgnoreCase) > 0)

        let report = if (Seq.isEmpty filteredItems) then "" else filteredItems |> Seq.map renderItem |> Seq.reduce concat2

        let title = Jira.getTitle()

        let mailto = sprintf "mailto:eng-core@gridgain.com?subject=%s" (title |> urlEncode)
        
        sprintf "<h1><a href='%s'>%s</a></h1>%s" mailto title report

