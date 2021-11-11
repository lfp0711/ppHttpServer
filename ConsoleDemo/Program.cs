using System;
using System.Collections.Generic;

namespace ppHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {

            Dictionary<String, String> _users = new Dictionary<string, string>();
            _users.Add("bit", "123456");
            HttpServer httpServer = new HttpServer(8088, new string[] { "hello" }, _users);

            httpServer.Start();

            Console.ReadLine();
        }
    }
}
