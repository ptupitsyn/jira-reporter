open System.Diagnostics
open JiraFsharpService
open System.ServiceProcess;

[<EntryPoint>]
let main argv = 
    
    // Uncomment to run in console
    //Process.Start("http://localhost:3443") |> ignore
    //WebServer.runWebServer()

    ServiceBase.Run [| new WindowsService() :> ServiceBase |]

    0 // return an integer exit code