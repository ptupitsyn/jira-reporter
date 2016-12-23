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
    let header = sprintf """ 
        <html>
            <head>
                <meta charset="utf-8" />
                <meta http-equiv="Pragma" content="no-cache"/>
                <title>%s</title>
                <style type="text/css">body{margin:20px 40px}</style>
            </head>
            <body> 
            <script>(function() { var spe = new SpeechSynthesisUtterance(); spe.text = "%s"; window.speechSynthesis.speak(spe); })(); 
            </script> """ (Jira.getTitle()) (Jira.getTitle())

    let footer = "</body></html>"

    let monitor = new Object()
    let mutable cachedReport : seq<ReportItem> = Seq.empty
    let mutable lastUpdated = DateTime.MinValue
    let mutable updateDuration = TimeSpan.Zero

    let getIssues includeAll = 
        match lastUpdated with
            | x when x > DateTime.Now.AddSeconds(-0.5) -> cachedReport
            | _ -> 
                let (issues, elapsed) = measureTime(fun() -> Jira.getCombinedIssues "-12h" includeAll)
                lastUpdated <- DateTime.Now
                updateDuration <- elapsed
                cachedReport <- issues
                cachedReport

    let getReport (showComments : bool) (personFilter : string) (includeAll : bool) =
        let issues = getIssues includeAll
        let reportBody = HtmlFormatter.renderReport issues showComments personFilter

        sprintf "%s%s<br/><br/><hr/><span style='font-size:small'>
            Use showComments=true&personFilter=name&includeAll=true in URL.
            <br/>Last updated at %A in %A%s. 
            <br/><a href='https://github.com/ptupitsyn/jira-reporter'>github.com/ptupitsyn/jira-reporter</a></span>" 
            header reportBody lastUpdated updateDuration footer

    let getReportSynced (showComments : bool) (personFilter : string) = 
        let getRep() = getReport showComments personFilter
        lock monitor getRep