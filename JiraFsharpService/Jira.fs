namespace JiraFsharpService

open System
open FSharp.Data

type JiraIssue = { Key : string; Summary : string; Status : string; Assignee : string; Url : string; Updated : DateTime  }
type ReportItem = { Person : string; Tasks : JiraIssue[]; Patches : JiraIssue[] }

module Jira = 


    [<Literal>]
    let JiraUrl = "https://issues.apache.org/jira/"

    [<Literal>]
    let ApiUrl = JiraUrl + "rest/api/2/"
    
    [<Literal>]
    let SampleUrl = ApiUrl + "search?jql=project=ignite&maxResults=10&expand=changelog"

    type Issues = JsonProvider<SampleUrl>

    let getAssignee (issue : Issues.Issue) = 
            match issue.Fields.Assignee with
                | Some(ass) -> ass.DisplayName
                | _ -> "Unassigned"

    let createIssue (this : Issues.Issue) =
        { 
            Key = this.Key; 
            Summary = this.Fields.Summary; 
            Status = this.Fields.Status.Name; 
            Assignee = getAssignee this; 
            Url = JiraUrl + "browse/" + this.Key; 
            Updated = this.Fields.Updated 
        }

    let getIssues period = 
        let url = sprintf "%ssearch?jql=project=ignite AND updated>%s AND status not in (open)&maxResults=100&expand=changelog" ApiUrl period
        let onReviewUrl = sprintf "%ssearch?jql=project=ignite AND status = 'Patch Available'&maxResults=100&expand=changelog" ApiUrl
        
        let jiraResult = Issues.Load url
        let onReview = Issues.Load onReviewUrl

        let historyIsPatch (hist : Issues.History) = 
            hist.Items |> Seq.exists (fun x -> (x.Field = "status" && x.ToString.String = Some("Patch Available")))

        let findPatchAuthor (issue : Issues.Issue) = 
            (issue.Changelog.Histories |> Seq.filter historyIsPatch |> Seq.last).Author.DisplayName

        let pendingPatches = 
            onReview.Issues 
                |> Seq.groupBy findPatchAuthor
                |> Map.ofSeq

        let getPatches person = 
            match pendingPatches.TryFind person with
                | Some(reviews) -> reviews |> Seq.map createIssue |> Seq.sortBy (fun x -> x.Updated) |> Array.ofSeq
                | _ -> [||]                

        let issues = jiraResult.Issues

        match issues with
            | [||] -> Seq.empty
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
                |> Seq.map (fun (person, issuePairs) -> 
                            { 
                                Person = person; 
                                Tasks = issuePairs |> Seq.map (snd>>createIssue) |> Array.ofSeq; 
                                Patches = getPatches person
                            })

    let getTitle() = DateTime.Now |> (fun dt -> (sprintf "DAILY STATUS (%i/%i/%i)" dt.Month dt.Day dt.Year))