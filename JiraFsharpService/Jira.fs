namespace JiraFsharpService

module Jira = 
    open FSharp.Data

    type Issues = JsonProvider<"https://issues.apache.org/jira/rest/api/latest/search?jql=project=ignite&maxResults=1&expand=changelog">

    let getIssues = 
        async {
            let! jiraResult = Issues.AsyncLoad "https://issues.apache.org/jira/rest/api/latest/search?jql=project=ignite AND updated>-12h AND status not in (open)&maxResults=50&expand=changelog"

            let concat acc x = acc + "<br />" + x
            let concat2 acc x = acc + "<br /><br />" + x
            let makeHeader x = "<h3>" + x + "</h3>"
            let makeLink text url = sprintf "<a href='%s'>%s</a>" url text
            let formatIssue (key, sum, status) = makeLink (key + " " + sum) (sprintf "https://issues.apache.org/jira/browse/%s" key) + " - " + status

            return
                jiraResult.Issues
                    |> Seq.collect (fun issue -> 
                        issue.Changelog.Histories 
                            |> Seq.map (fun hist -> hist.Author.DisplayName)
                            |> Seq.distinct
                            |> Seq.map (fun author -> (author, (issue.Key, issue.Fields.Summary, issue.Fields.Status.Name)))
                        )
                    |> Seq.groupBy (fun (person, ticket) -> makeHeader person)
                    |> Seq.map (fun (person, issues) -> concat person (issues |> Seq.map (snd >> formatIssue) |> Seq.reduce concat))
                    |> Seq.reduce concat2
        }