open JiraFsharpService
open FSharp.Data

type Issue = JsonProvider<"https://issues.apache.org/jira/rest/api/latest/issue/IGNITE-1">

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

(*
    let jira = Jira("https://issues.apache.org/jira", "IGNITE", None)
    let res = jira.RunQuery "issue/IGNITE-100"
    printf "%A" res
    *)

    let json = Issue.Load("https://issues.apache.org/jira/rest/api/latest/issue/IGNITE-2699")    

    printf "%A" json.Fields.Summary

    0 // return an integer exit code
