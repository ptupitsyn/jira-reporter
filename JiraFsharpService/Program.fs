open System.Diagnostics
open JiraFsharpService
open System.ServiceProcess;
open System.Threading;

[<EntryPoint>]
let main argv = 
    
    // Uncomment to run in console
    (*
    Process.Start("http://localhost:3443/?showComments=false&personFilter=tupitsyn") |> ignore
    WebServer.runWebServer()
    Thread.Sleep(Timeout.Infinite)
    *)

    ServiceBase.Run [| new WindowsService() :> ServiceBase |]

    0 // return an integer exit code