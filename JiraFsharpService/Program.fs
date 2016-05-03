open JiraFsharpService
open System
open System.Diagnostics
open Suave
open Suave.Successful

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

    let monitor = new Object();
    let mutable cachedReport = ""
    let mutable lastUpdated = DateTime.MinValue

    let getReport () =                 
        match lastUpdated with
            | x when x > DateTime.Now.AddSeconds(-5.0) -> cachedReport
            | _ -> 
                let issues = Jira.getIssues "-12h"
                let title = Jira.getTitle()
                lastUpdated <- DateTime.Now
                cachedReport <- sprintf "<h1>%s</h1>%s<br/><hr/>Last updated: %A" title issues lastUpdated
                cachedReport

    let getReportSynced () = lock monitor getReport

    let getReportWeb : WebPart = 
        fun (ctx : HttpContext) -> 
            let html = getReportSynced()
            OK html ctx

    Process.Start("http://localhost:8083") |> ignore

    startWebServer defaultConfig getReportWeb

    0 // return an integer exit code
