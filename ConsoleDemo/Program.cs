using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace ppHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {

            Dictionary<String, String> _users = new Dictionary<string, string>();
            _users.Add("tom", "123456");
            HttpServer httpServer = new HttpServer(8080, new string[] { "hello" }, _users);

            httpServer.Logger = log2Console;
            httpServer.HandleRequest = myHandleRequest;

            httpServer.Start();

            while (true)
            {
                Console.Write("Enter Command (Start, Stop, or Exit): ");
                var input = Console.ReadLine();
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else if (input.Equals("start", StringComparison.OrdinalIgnoreCase))
                {
                    httpServer.Start();
                }
                else if (input.Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    httpServer.Stop();
                }
                else
                {
                    Console.WriteLine("Unknown Command!");
                }
            }

            httpServer.Stop();

        }


        private static byte[] myHandleRequest(HttpListenerRequest request, HttpListenerResponse response, String authUser)
        {
            log2Console("myHandleRequest");
            String respBody = null;

            String method = request.HttpMethod;
            Uri url = request.Url;
            String path = url.AbsolutePath;

            if (path == "/hello")
            {
                Encoding encoding = request.ContentEncoding;
                Stream input = request.InputStream;
                StreamReader reader = new StreamReader(input, encoding);

                String name = request.QueryString.Get("name");

                String responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                if (name != null)
                {
                    responseString = "<HTML><BODY> Hello " + name + "! <br>Date time: " + DateTime.UtcNow + " </BODY></HTML>";
                }

                response.ContentType = MediaTypeNames.Text.Html;

                respBody = responseString;

                encoding = response.ContentEncoding;
                if (encoding == null)
                {
                    encoding = Encoding.UTF8;
                    response.ContentEncoding = encoding;
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(respBody);

            return buffer;
        }

        private static void log2Console(string? message)
        {
            Console.WriteLine(DateTime.UtcNow + " " + message);
        }
    }
}
