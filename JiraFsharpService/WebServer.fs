namespace JiraFsharpService

module WebServer =
    open System.Net
    open Suave
    open Suave.Successful

    let runWebServer() =
        let getReportWeb : WebPart = 
            fun (ctx : HttpContext) ->                 
                let showComments = match ctx.request.queryParam "showComments" with
                                        | Choice1Of2 x -> x = "true"
                                        | _ -> false

                let personFilter = match ctx.request.queryParam "personFilter" with
                                        | Choice1Of2 x -> x
                                        | _ -> ""
                
                let includeAll = match ctx.request.queryParam "includeAll" with
                                        | Choice1Of2 x -> x = "true"
                                        | _ -> false

                let html = Reporter.getReportSynced showComments personFilter includeAll
                OK html ctx

        let suaveCfg = { defaultConfig with bindings = [ HttpBinding.mk HTTP IPAddress.Any 3443us ] }

        let startingServer, shutdownServer = startWebServerAsync suaveCfg getReportWeb

        Async.Start(shutdownServer)
