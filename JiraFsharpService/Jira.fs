namespace JiraFsharpService
open System
open System.IO
open System.Net

type Jira(url : string, project : string, creds) = 
    member this.Url = url
    member this.Project = project
    member this.Creds = creds
    member this.ApiUrl = url + "/rest/api/latest/"
    
    member this.GetAuthorizedClient() = 
        let wc = new WebClient()

        wc.Headers.[HttpRequestHeader.Authorization] <- 
            match creds with
                | Some(usr, pwd) -> sprintf "%s:%s" usr pwd
                | None -> null

        wc
    
    member this.RunQuery(query : string) = 
        this.GetAuthorizedClient().DownloadString this.ApiUrl + query