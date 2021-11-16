using ppHttpServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Windows;

namespace WpfDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpServer httpServer = null;

        public MainWindow()
        {
            InitializeComponent();

            Prefixes.AppendText("/hello" + Environment.NewLine);

            Users.AppendText("tom/123456" + Environment.NewLine);
            Users.AppendText("jack/123456" + Environment.NewLine);

            httpServer = new HttpServer(8080)
            {
                Logger = log2Text,
                HandleRequest = myHandleRequest
            };

            Start.IsEnabled = true;
            Stop.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            log2Text("Start_Click");
            if (httpServer != null)
            {
                int port = int.Parse(Port.Text);
                List<String> prefixList = new List<String>();
                foreach (String line in Prefixes.Text.Split(Environment.NewLine))
                {
                    Debug.Print(line);
                    if (!String.IsNullOrEmpty(line))
                        prefixList.Add(line);
                }
                String[] pathPrefixes = prefixList.ToArray();

                Dictionary<String, String> userMap = new Dictionary<String, String>();
                foreach (String line in Users.Text.Split(Environment.NewLine))
                {
                    Debug.Print(line);
                    String[] strs = line.Split("/");
                    if (strs.Length == 2)
                    {
                        if (strs[0].Length > 0 && strs[1].Length > 0)
                            userMap.Add(strs[0], strs[1]);
                    }
                }

                httpServer.Port = port;
                httpServer.PathPrefixes = pathPrefixes;
                httpServer.Users = userMap;

                httpServer.Start();

                Start.IsEnabled = false;
                Stop.IsEnabled = true;
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            log2Text("Stop_Click");
            if (httpServer != null)
            {
                httpServer.Stop();

                Start.IsEnabled = true;
                Stop.IsEnabled = false;
            }
        }

        private byte[] myHandleRequest(HttpListenerRequest request, HttpListenerResponse response, String authUser)
        {
            log2Text("myHandleRequest");
            String respBody = null;

            String method = request.HttpMethod;
            Uri url = request.Url;
            String path = url.AbsolutePath;

            if (path == "/hello")
            {
                Thread.Sleep(1 * 1000);

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

        private void log2Text(string? message)
        {
            LogTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                LogTextBox.Text += DateTime.UtcNow + " " + message + Environment.NewLine;
            }));
        }
    }
}
