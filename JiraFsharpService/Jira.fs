namespace JiraFsharpService

module Jira = 
    open FSharp.Data
    open System

    type Issues = JsonProvider<"https://issues.apache.org/jira/rest/api/latest/search?jql=project=ignite&maxResults=1&expand=changelog">

    let getIssues period = 
        let url = sprintf "https://issues.apache.org/jira/rest/api/latest/search?jql=project=ignite AND updated>%s AND status not in (open)&maxResults=50&expand=changelog" period
        let jiraResult = Issues.Load url

        let concat acc x = acc + "<br />" + x
        let concat2 acc x = acc + "<br /><br />" + x
        let makeHeader x = "<h3>" + x + "</h3>"
        let makeLink text url = sprintf "<a href='%s'>%s</a>" url text
        let formatIssue (key, sum, status) = makeLink (key + " " + sum) (sprintf "https://issues.apache.org/jira/browse/%s" key) + " - " + status

        let issues = jiraResult.Issues

        match issues with
            | [||] -> "No issues found for the given period: " + period
            | x -> x 
                |> Seq.collect (fun issue -> 
                    issue.Changelog.Histories 
                        |> Seq.where (fun hist -> (System.DateTime.Now - hist.Created).Hours < 12)
                        |> Seq.map (fun hist -> hist.Author.DisplayName)
                        |> Seq.distinct
                        |> Seq.map (fun author -> (author, (issue.Key, issue.Fields.Summary, issue.Fields.Status.Name)))
                    )
                |> Seq.groupBy (fun (person, _) -> makeHeader person)
                |> Seq.sortBy (fun (person, _) -> person)
                |> Seq.map (fun (person, issues) -> person + (issues |> Seq.map (snd >> formatIssue) |> Seq.reduce concat))
                |> Seq.reduce concat2

    let getTitle() = DateTime.Now |> (fun dt -> (sprintf "DAILY STATUS (%i/%i/%i)" dt.Month dt.Day dt.Year))