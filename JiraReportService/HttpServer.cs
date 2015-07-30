using System;
using System.Net;
using System.Text;

namespace JiraReportService
{
    public static class HttpServer
    {
        public static void Run(Func<HttpListenerRequest, string> requestProcessor, string prefix)
        {
            var listener = new HttpListener { Prefixes = { prefix } };
            listener.Start();

            while (true)
            {
                // URI prefixes are required, 
                // for example "http://contoso.com:8080/index/".

                // Create a listener.
                var context = listener.GetContext();  // this blocks
                var request = context.Request;

                // Obtain a response object.
                var response = context.Response;

                // Construct a response. 
                var responseString = requestProcessor(request);
                var buffer = Encoding.UTF8.GetBytes(responseString);

                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                // You must close the output stream.
                output.Close();
            }
        }
    }
}