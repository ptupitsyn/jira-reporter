open JiraFsharpService
open System
open Suave
open Suave.Filters
open Suave.Successful
open Suave.Operators

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

    let monitor = new Object();
    let mutable cachedReport = ""
    let mutable lastUpdated = DateTime.MinValue

    let getReport () = 
        lock monitor (fun () -> 
                match lastUpdated with
                    | x when x > DateTime.Now.AddSeconds(-5.0) -> cachedReport
                    | _ -> 
                        let issues = Jira.getIssues "-12h"
                        let title = Jira.getTitle()
                        lastUpdated <- DateTime.Now
                        cachedReport <- sprintf "<h1>%s</h1>%s<br/><hr/>Last updated: %A" title issues lastUpdated
                        cachedReport
            )

    let getReportWeb : WebPart = 
        fun (ctx : HttpContext) -> 
            let html = getReport()
            OK html ctx

    System.Diagnostics.Process.Start("http://localhost:8083") |> ignore

    startWebServer defaultConfig getReportWeb

    0 // return an integer exit code
