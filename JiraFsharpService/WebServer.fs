namespace JiraFsharpService

module WebServer =
    open System.Net
    open Suave
    open Suave.Successful

    let runWebServer() =
        let getReportWeb : WebPart = 
            fun (ctx : HttpContext) -> 
                let html = Reporter.getReportSynced()
                OK html ctx

        let suaveCfg = { defaultConfig with bindings = [ HttpBinding.mk HTTP IPAddress.Loopback 3443us ] }

        let startingServer, shutdownServer = startWebServerAsync suaveCfg getReportWeb

        Async.Start(shutdownServer)
