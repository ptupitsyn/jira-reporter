﻿open JiraFsharpService
open System
open System.Diagnostics
open Suave
open Suave.Successful

let measureTime f =
    let sw = Stopwatch.StartNew()
    let res = f()
    let elapsed = sw.Elapsed
    (res, elapsed)

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"
    
    // TODO: Extract HTML logic to a separate module
    let header = """ 
        <html>
            <head>
                <meta charset="utf-8" />
                <title>JIRA Report</title>
                <style type="text/css">body{margin:20px 40px}</style>
            </head>
            <body> """

    let footer = "</body></html>"

    let monitor = new Object()
    let mutable cachedReport = ""
    let mutable lastUpdated = DateTime.MinValue

    let getReport() =                 
        match lastUpdated with
            | x when x > DateTime.Now.AddSeconds(-5.0) -> cachedReport
            | _ -> 
                let (issues, elapsed) = measureTime(fun() -> Jira.getIssues "-12h")
                let title = Jira.getTitle()
                lastUpdated <- DateTime.Now

                cachedReport <- sprintf "%s<h1>%s</h1>%s<br/><hr/>Last updated at %A in %A%s" header title issues lastUpdated elapsed footer
                cachedReport

    let getReportSynced() = lock monitor getReport

    let getReportWeb : WebPart = 
        fun (ctx : HttpContext) -> 
            let html = getReportSynced()
            OK html ctx

    Process.Start("http://localhost:8083") |> ignore

    startWebServer defaultConfig getReportWeb

    0 // return an integer exit code