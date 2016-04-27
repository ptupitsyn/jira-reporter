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
                return! OK Jira.getIssues ctx
            }

    startWebServer defaultConfig getReport

    0 // return an integer exit code
