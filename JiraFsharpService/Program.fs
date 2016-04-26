open JiraFsharpService
open FSharp.Data

type Jira = JsonProvider<"https://issues.apache.org/jira/rest/api/latest/search?filter=-4">

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

    let jql = "project in (IGNITE) AND updated>-12h AND status not in (open)"

    Jira.GetSample().Issues |> Array.map (fun issue -> (issue.Key + ": " + issue.Fields.Summary)) |> printf "%A"

    // TODO: https://suave.io

    0 // return an integer exit code
