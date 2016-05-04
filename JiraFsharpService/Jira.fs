namespace JiraFsharpService

module Jira = 
    open FSharp.Data
    open System

    [<Literal>]
    let apiUrl = "https://issues.apache.org/jira/rest/api/2/"
    
    [<Literal>]
    let sampleUrl = apiUrl + "search?jql=project=ignite&maxResults=10&expand=changelog"

    type Issues = JsonProvider<sampleUrl>

    let getIssues period = 
        let url = sprintf "%ssearch?jql=project=ignite AND updated>%s AND status not in (open)&maxResults=100&expand=changelog" apiUrl period
        let onReviewUrl = sprintf "%ssearch?jql=project=ignite AND status = 'Patch Available'&maxResults=100&expand=changelog" apiUrl
        
        let jiraResult = Issues.Load url
        let onReview = Issues.Load onReviewUrl

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

        let getAssignee (issue : Issues.Issue) = 
            match issue.Fields.Assignee with
                | Some(ass) -> ass.DisplayName
                | _ -> "Unassigned"

        let getWaitTime (issue : Issues.Issue) = 
            sprintf "%s (waiting %i days)" (getAssignee issue) (int (DateTime.Now - issue.Fields.Updated).TotalDays)

        let formatIssueEx includeStatus (issue : Issues.Issue) = 
            let summary = issue.Key + " " + issue.Fields.Summary
            let url = sprintf "https://issues.apache.org/jira/browse/%s" issue.Key
            makeLink summary url + " - " + (if includeStatus then formatStatus issue.Fields.Status.Name else getWaitTime issue)

        let formatIssue = formatIssueEx true
        let formatIssueNoStatus = formatIssueEx false

        let historyIsPatch (hist : Issues.History) = 
            hist.Items |> Seq.exists (fun x -> (x.Field = "status" && x.ToString.String = Some("Patch Available")))

        let findPatchAuthor (issue : Issues.Issue) = 
            (issue.Changelog.Histories |> Seq.filter historyIsPatch |> Seq.last).Author.DisplayName

        let pendingPatches = 
            onReview.Issues 
                |> Seq.groupBy findPatchAuthor
                |> Map.ofSeq

        let issues = jiraResult.Issues

        match issues with
            | [||] -> "No issues found for the given period: " + period
            | x -> x 
                |> Seq.collect (fun issue -> 
                    issue.Changelog.Histories 
                        |> Seq.where (fun hist -> (System.DateTime.Now - hist.Created).Hours < 12)
                        |> Seq.map (fun hist -> hist.Author.DisplayName)
                        |> Seq.distinct
                        |> Seq.map (fun author -> (author, issue))
                    )
                |> Seq.groupBy (fun (person, _) -> person)
                |> Seq.sortBy (fun (person, _) -> person)
                |> Seq.map 
                    (fun (person, issuePairs) -> 
                        let issues = issuePairs |> Seq.map snd |> List.ofSeq
                        let reviews = match pendingPatches.TryFind person with
                            | Some(reviews) -> concat "<br/><br/>Pending Patches" (reviews |> Seq.sortBy (fun x -> x.Fields.Updated) |> Seq.map formatIssueNoStatus |> Seq.reduce concat)
                            | _ -> ""
                        (makeHeader person) + (issues |> Seq.map formatIssue |> Seq.reduce concat) + reviews
                    )
                |> Seq.reduce concat2

    let getTitle() = DateTime.Now |> (fun dt -> (sprintf "DAILY STATUS (%i/%i/%i)" dt.Month dt.Day dt.Year))