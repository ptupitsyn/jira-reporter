namespace JiraFsharpService
    
open System.ServiceProcess;

type public WindowsService() =
    inherit ServiceBase(ServiceName = "JiraFSharpService")

    override x.OnStart(args) =
        WebServer.runWebServer() |> ignore