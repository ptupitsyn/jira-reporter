open JiraFsharpService
open Suave
open Suave.Filters
open Suave.Successful
open Suave.Operators

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

    let getReport : WebPart =
        fun (ctx : HttpContext) ->
            async {
                let! html = Jira.getIssues
                return! OK html ctx
            }

    System.Diagnostics.Process.Start("http://localhost:8083") |> ignore

    startWebServer defaultConfig getReport

    0 // return an integer exit code
