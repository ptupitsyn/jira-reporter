namespace JiraFsharpService

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Net
open System.Threading
open System.Threading.Tasks
open FSharp.Data

type JiraIssue = { Key : string; Summary : string; Status : string; Assignee : string; Url : string; Updated : DateTime; Parent : JiraIssue option; Comment : string  }
type ReportItem = { Person : string; Tasks : JiraIssue[]; Patches : JiraIssue[] }

module Jira = 
    [<Literal>]
    let JiraUrl = "https://issues.apache.org/jira/"

    [<Literal>]
    let ApiUrl = JiraUrl + "rest/api/2/"

    [<Literal>]
    let ExpandParams = "&expand=changelog&fields=summary,status,comment,assignee,parent,updated"
    
    [<Literal>]
    let SampleUrl = ApiUrl + "search?jql=project=ignite&maxResults=10" + ExpandParams

    type Issues = JsonProvider<SampleUrl>
    
    let createIssue (this : Issues.Issue) =
        let getAssignee (issue : Issues.Issue) = 
            match issue.Fields.Assignee with
                | Some(ass) -> ass.DisplayName
                | _ -> "Unassigned"

        let getLastComment (issue : Issues.Issue) = 
            match issue.Fields.Comment.Comments with
                | [||] -> ""
                | arr -> arr |> Seq.last |> 
                            fun x -> if (x.Created - DateTime.Now).TotalHours < 12.0 then x.Body else ""

        let createParentIssue (this : Issues.Parent) =
            {
                Key = this.Key;
                Summary = this.Fields.Summary;
                Status = this.Fields.Status.Name;
                Assignee = "-";
                Url = JiraUrl + "browse/" + this.Key;
                Updated = DateTime.Now;
                Parent =  None;
                Comment = ""
            }

        { 
            Key = this.Key; 
            Summary = this.Fields.Summary; 
            Status = this.Fields.Status.Name; 
            Assignee = getAssignee this; 
            Url = Regex.Match(this.Self, "(.*?)rest/api").Groups.[1].Value + "browse/" + this.Key; 
            Updated = this.Fields.Updated;
            Parent = match this.Fields.Parent with
                            | Some(parent) -> Some(createParentIssue parent)
                            | _ -> None
            Comment = getLastComment this
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

    let getIgniteIssues period = 
        let url = sprintf "%ssearch?jql=project=ignite AND updated>%s AND status != open&maxResults=100%s" ApiUrl period ExpandParams
        let onReviewUrl = sprintf "%ssearch?jql=project=ignite AND status = 'Patch Available'&maxResults=100%s" ApiUrl ExpandParams
        
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
                        |> Seq.where (fun hist -> (System.DateTime.Now - hist.Created).TotalHours < 12.0)  // TODO: period is ignored!
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

    let getEncodedCreds() = 
        let creds = File.ReadAllLines "c:\\jira.jira"
        let mergedCreds = sprintf "%s:%s" creds.[0] creds.[1]
        let byteCreds = Encoding.UTF8.GetBytes mergedCreds
        Convert.ToBase64String byteCreds

    let getGgIssuesRaw (period : string) = 
        let wc = new WebClient()
        let creds = "Basic " + getEncodedCreds()
        wc.Headers.Add(HttpRequestHeader.Authorization, creds)
        let url = sprintf "https://ggsystems.atlassian.net/rest/api/2/search?jql=project=gg AND updated>%s AND status != open&maxResults=100%s" period ExpandParams
        wc.DownloadString url

    let getGgIssues period = 
        let json = getGgIssuesRaw period

        // TODO: Pages
        let jiraResult = (Issues.Parse json).Issues

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
                                Patches = [||]
                            })

    let getCombinedIssues period = 
        let igniteTask = Task.Factory.StartNew(fun () -> getIgniteIssues period)
        let ggTask = Task.Factory.StartNew(fun () -> getGgIssues period)

        Task.WaitAll(igniteTask, ggTask)

        let ignite = igniteTask.Result
        let gg = ggTask.Result

        ignite |> Seq.append gg 
            |> Seq.groupBy (fun i -> i.Person)
            |> Seq.map (fun g -> 
                        {
                            Person = fst g;
                            Tasks = (snd g) |> Seq.collect (fun x -> x.Tasks) |> Array.ofSeq;
                            Patches = (snd g) |> Seq.collect (fun x -> x.Patches) |> Array.ofSeq;
                        })
            |> Seq.sortBy (fun r -> r.Person)

    let getTitle() = DateTime.Now |> (fun dt -> (sprintf "DAILY STATUS (%i/%i/%i)" dt.Month dt.Day dt.Year))