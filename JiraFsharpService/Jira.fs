namespace JiraFsharpService

open System
open FSharp.Data

type JiraIssue = { Key : string; Summary : string; Status : string; Assignee : string; Url : string; Updated : DateTime; Parent : JiraIssue option  }
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

    let createParentIssue (this : Issues.Parent) =
        {
            Key = this.Key;
            Summary = this.Fields.Summary;
            Status = this.Fields.Status.Name;
            Assignee = "-";
            Url = JiraUrl + "browse/" + this.Key;
            Updated = DateTime.Now;
            Parent =  None;
        }
    
    let createIssue (this : Issues.Issue) =
        { 
            Key = this.Key; 
            Summary = this.Fields.Summary; 
            Status = this.Fields.Status.Name; 
            Assignee = getAssignee this; 
            Url = JiraUrl + "browse/" + this.Key; 
            Updated = this.Fields.Updated;
            Parent = match this.Fields.Parent with
                            | Some(parent) -> Some(createParentIssue parent)
                            | _ -> None
        }

    let loadAllIssues (url : string) = 
        let initial = Issues.Load url
        let pageSize = initial.MaxResults
        let pages = {1..(initial.Total / initial.MaxResults)}
        let res = 
            pages 
                |> Seq.map (fun page -> Issues.Load (sprintf "%s&startAt=%i" url (page * pageSize)))
                |> Seq.collect (fun x->x.Issues)
                |> Seq.append initial.Issues
                |> Array.ofSeq
        assert (initial.Total = res.Length)  // check that all pages are loaded
        res

    let getIssues period = 
        // TODO: Extract method with custom JQL
        let url = sprintf "%ssearch?jql=project=ignite AND updated>%s AND status not in (open)&maxResults=100&expand=changelog" ApiUrl period
        let onReviewUrl = sprintf "%ssearch?jql=project=ignite AND status = 'Patch Available'&maxResults=100&expand=changelog" ApiUrl
        
        let jiraResult = loadAllIssues url
        let onReview = loadAllIssues onReviewUrl

        let historyIsPatch (hist : Issues.History) = 
            hist.Items |> Seq.exists (fun x -> (x.Field = "status" && x.ToString.String = Some("Patch Available")))

        let findPatchAuthor (issue : Issues.Issue) = 
            (issue.Changelog.Histories |> Seq.filter historyIsPatch |> Seq.last).Author.DisplayName

        let pendingPatches = 
            onReview 
                |> Seq.groupBy findPatchAuthor
                |> Map.ofSeq

        let getPatches person = 
            match pendingPatches.TryFind person with
                | Some(reviews) -> reviews |> Seq.map createIssue |> Seq.sortBy (fun x -> x.Updated) |> Array.ofSeq
                | _ -> [||]                

        match jiraResult with
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