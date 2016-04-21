open JiraFsharpService

[<EntryPoint>]
let main argv = 
    printfn "Starting Jira test"

    let jira = Jira("https://issues.apache.org/jira", "IGNITE", None)

    let res = jira.RunQuery "issue/IGNITE-100"

    printf "%A" res

    0 // return an integer exit code
