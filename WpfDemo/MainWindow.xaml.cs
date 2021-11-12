using ppHttpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Dictionary<String, String> _users = new Dictionary<string, string>();
        HttpServer httpServer = null;

        public MainWindow()
        {
            InitializeComponent();

            _users.Add("bit", "123456");

            httpServer = new HttpServer(8080, new string[] { "hello" }, _users);

            httpServer.Logger = log2Text;
            httpServer.HandleRequest = myHandleRequest;

            Start.IsEnabled = true;
            Stop.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            log2Text("Start_Click");
            if (httpServer != null)
            {
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
                Encoding encoding = request.ContentEncoding;
                Stream input = request.InputStream;
                StreamReader reader = new StreamReader(input, encoding);

                String name = request.QueryString.Get("name");

                String responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                if (name != null)
                {
                    responseString = "<HTML><BODY> Hello " + name + "! </BODY></HTML>";
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
            LogTextBlock.Text += DateTime.UtcNow + " " + message + Environment.NewLine;
        }
    }
}
