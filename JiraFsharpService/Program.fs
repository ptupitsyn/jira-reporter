open System.Diagnostics
open JiraFsharpService

[<EntryPoint>]
let main argv = 
    Process.Start("http://localhost:3443") |> ignore

    WebServer.runWebServer()

    0 // return an integer exit code