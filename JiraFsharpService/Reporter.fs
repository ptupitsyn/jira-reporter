namespace JiraFsharpService

module Reporter =
    open JiraFsharpService
    open System
    open System.Diagnostics

    let measureTime f =
        let sw = Stopwatch.StartNew()
        let res = f()
        let elapsed = sw.Elapsed
        (res, elapsed)

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
                let (issues, elapsed) = measureTime(fun() -> Jira.getCombinedIssues "-12h")
                lastUpdated <- DateTime.Now

                let reportBody = HtmlFormatter.renderReport issues

                cachedReport <- sprintf "%s%s<br/><br/><hr/><span style='font-size:small'>Last updated at %A in %A%s. 
                    <br/><a href='https://github.com/ptupitsyn/jira-reporter'>github.com/ptupitsyn/jira-reporter</a></span>" 
                    header reportBody lastUpdated elapsed footer
                
                cachedReport

    let getReportSynced() = lock monitor getReport

