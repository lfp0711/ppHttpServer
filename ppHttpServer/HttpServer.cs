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

        private int _port;
        private String[] _pathPrefixes = new String[] { };
        private Dictionary<String, String> _users = new Dictionary<String, String>();

        private Func<HttpListenerRequest, HttpListenerResponse, String, byte[]> _handleRequest;

        private Action<String> _logger;

        private HttpListener _listener;

        public Func<HttpListenerRequest, HttpListenerResponse, string, byte[]> HandleRequest { get => _handleRequest; set => _handleRequest = value; }
        public Action<string> Logger { get => _logger; set => _logger = value; }
        public int Port { get => _port; set { if (_port != value) { _port = value; handlePrefixes(); } } }
        public string[] PathPrefixes { get => _pathPrefixes; set { _pathPrefixes = value; if (_pathPrefixes == null) _pathPrefixes = new String[] { }; handlePrefixes(); } }

        public Dictionary<string, string> Users { get => _users; set { _users = value; if (_users == null) _users = new Dictionary<String, String>();  handleAuth(); } }

        public HttpServer() : this(DEFAULT_PORT, null, null) { }

        public HttpServer(int port) : this(port, new String[] { }, null) { }

        public HttpServer(int port, String[] paths) : this(port, paths, null) { }
        public HttpServer(int port, Dictionary<String, String> users) : this(port, null, users) { }

        public HttpServer(int port, String[] pathPrefixes, Dictionary<String, String> users)
        {
            this.Port = port;
            this.PathPrefixes = pathPrefixes;
            this.Users = users;

            Init();
        }


        private void Init()
        {
            _log("Init Begin");
            _listener = new HttpListener();

            handlePrefixes();
            handleAuth();

            _handleRequest = demoHandleRequest;
            _logger = log2Debug;

            _log("Init End");
        }

        private void handlePrefixes()
        {
            _log("Handle Prefixes");
            if (_listener != null)
            {
                List<string> prefixList = new List<string>();
                if (PathPrefixes.Length == 0)
                {
                    prefixList.Add(string.Format("http://*:{0}/", Port));
                }
                else
                {
                    for (int i = 0; i < PathPrefixes.Length; i++)
                    {
                        var _pathPrefix = PathPrefixes[i];
                        if(!String.IsNullOrEmpty(_pathPrefix.Trim()))
                        {
                            if (_pathPrefix.StartsWith("/"))
                            {
                                _pathPrefix = _pathPrefix.Substring(1);
                            }
                            if (!_pathPrefix.EndsWith("/"))
                            {
                                _pathPrefix = _pathPrefix + "/";
                            }
                            prefixList.Add(string.Format("http://*:{0}/{1}", Port, _pathPrefix));
                        }
                    }
                }

                _listener.Prefixes.Clear();
                prefixList.ForEach(delegate (string prefix)
                {
                    _log("prefix: " + prefix);
                    _listener.Prefixes.Add(prefix);
                });
            }
        }
        private void handleAuth()
        {
            _log("Handle Auth");
            if (_listener != null)
            {
                if (Users.Count > 0)
                    _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                else
                    _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            }
        }

        public void Start()
        {
            _log("Start HttpServer");

            _listener.Start();
            BeginGetContext();
        }

        public void Stop()
        {
            _log("Stop HttpServer");
            _listener.Stop();
        }

        private void BeginGetContext()
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.BeginGetContext(GetContextCallback, _listener);
            }
        }

        private void GetContextCallback(IAsyncResult asyncResult)
        {
            HttpListenerContext context = null;
            try
            {
                context = _listener.EndGetContext(asyncResult);

                OnRequest(context);
            }
            catch
            {
                //_log("" + e);
            }
            finally
            {
                BeginGetContext();
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

                        if (Users.ContainsKey(identity.Name) && Users[identity.Name].Equals(identity.Password))
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
