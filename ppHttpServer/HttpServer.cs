using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace ppHttpServer
{
    public class HttpServer
    {
        private const int DEFAULT_PORT = 8080;

        private int _port = 0;
        private String[] _paths = new string[] { };
        private Dictionary<String, String> _users = new Dictionary<string, string>();

        private volatile bool _keepGoing = true;
        private Task _handleTask;

        private Func<HttpListenerRequest, HttpListenerResponse, String, byte[]> _handleRequest;

        private Action<String> _logger;

        private HttpListener _listener;

        public Func<HttpListenerRequest, HttpListenerResponse, string, byte[]> HandleRequest { get => _handleRequest; set => _handleRequest = value; }
        public Action<string> Logger { get => _logger; set => _logger = value; }

        public HttpServer() : this(DEFAULT_PORT) { }

        public HttpServer(int port) : this(port, null) { }

        public HttpServer(int port, String[] paths) : this(port, paths, null) { }

        public HttpServer(int port, String[] paths, Dictionary<String, String> users)
        {
            this._port = port;
            if (paths != null)
                this._paths = paths;
            if (users != null)
                this._users = users;

            Init();
        }



        private void Init()
        {
            _log("Init Begin");
            _listener = new HttpListener();

            List<string> prefixList = new List<string>();

            if (_paths.Length == 0)
            {
                prefixList.Add(string.Format("http://*:{0}/", _port));
            }
            else
            {
                for (int i = 0; i < _paths.Length; i++)
                {
                    var path = _paths[i];
                    if (path.StartsWith("/"))
                    {
                        path = path.Substring(1);
                    }
                    if (!path.EndsWith("/"))
                    {
                        path = path + "/";
                    }
                    prefixList.Add(string.Format("http://*:{0}/{1}", _port, path));
                }
            }
            prefixList.ForEach(delegate (string prefix)
            {
                _log(prefix);
                _listener.Prefixes.Add(prefix);
            });

            if (_users.Count > 0)
                _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;

            _handleRequest = demoHandleRequest;
            _logger = log2Debug;

            _log("Init End");
        }



        public void Start()
        {
            _log("Start HttpServer");
            if (_handleTask != null && !_handleTask.IsCompleted) return; //Already started
            _handleTask = HandleTask();
        }

        private async Task HandleTask()
        {
            _log("HandleTask Started");
            try
            {
                _listener.Start();
                _log("HttpServer - started. Will listen on port " + _port);

                while (_keepGoing)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        lock (_listener)
                        {
                            if (_keepGoing) OnRequest(context);
                        }
                    }
                    catch (Exception ex)
                    {
                        //_log("" + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log("" + ex);
            }
        }
        public void Stop()
        {
            _log("Stop HttpServer");
            _keepGoing = false;
            try
            {
                lock (_listener)
                {
                    _listener.Stop();
                }
                _handleTask.Wait();
            }
            catch (Exception ex)
            {
                _log("" + ex);
            }
        }

        private void OnRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            String method = request.HttpMethod;
            Uri url = request.Url;
            String path = url.AbsolutePath;
            String authUser = auth(context);

            _log("OnRequest: " + (authUser == null ? "-" : authUser) + " " + request.RemoteEndPoint.Address + " " + method + " " + url + "");

            if (_listener.AuthenticationSchemes == AuthenticationSchemes.Basic && authUser == null)
            {
                response.StatusCode = ((int)HttpStatusCode.Unauthorized);
            }
            else
            {
                if (HandleRequest != null)
                {
                    byte[] buffer = HandleRequest(request, response, authUser);
                    if (buffer != null)
                    {
                        // Get a response stream and write the response to it.
                        response.ContentLength64 = buffer.Length;
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);

                        output.Flush();
                    }
                }
            }

            response.Close();
        }

        private String auth(HttpListenerContext context)
        {
            try
            {
                if (_listener.AuthenticationSchemes == AuthenticationSchemes.Basic)
                {
                    if (context.User != null)
                    {
                        HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;

                        if (_users.ContainsKey(identity.Name) && _users[identity.Name].Equals(identity.Password))
                            return identity.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _log("" + ex);
            }

            return null;
        }

        private byte[] demoHandleRequest(HttpListenerRequest request, HttpListenerResponse response, String authUser)
        {
            String respBody = null;

            String method = request.HttpMethod;
            Uri url = request.Url;
            String path = url.AbsolutePath;

            if (path == "/hello")
            {
                Encoding encoding = request.ContentEncoding;
                Stream input = request.InputStream;
                StreamReader reader = new StreamReader(input, encoding);

                String requestBody = reader.ReadToEnd();

                string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
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

        private void log2Debug(string? message)
        {
            Debug.Print(DateTime.UtcNow + " " + message);
        }
        private void _log(string? message)
        {
            if (_logger != null)
                _logger(message);
        }

    }
}
