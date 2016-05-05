open JiraFsharpService
open System
open System.Net
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
                lastUpdated <- DateTime.Now

                let reportBody = HtmlFormatter.renderReport issues

                cachedReport <- sprintf "%s%s<br/><br/><hr/><span style='font-size:small'>Last updated at %A in %A%s</span>" 
                    header reportBody lastUpdated elapsed footer
                
                cachedReport

    let getReportSynced() = lock monitor getReport

    let getReportWeb : WebPart = 
        fun (ctx : HttpContext) -> 
            let html = getReportSynced()
            OK html ctx

    Process.Start("http://localhost:3443") |> ignore

    let suaveCfg = { defaultConfig with bindings = [ HttpBinding.mk HTTP IPAddress.Loopback 3443us ] }

    startWebServer suaveCfg getReportWeb

    0 // return an integer exit code