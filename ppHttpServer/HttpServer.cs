using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ppHttpServer
{
    public class HttpServer
    {
        private const int DEFAULT_PORT = 8080;

        private int _port = 0;
        private String[] _paths = new string[] { };
        private Dictionary<String, String> _users = new Dictionary<string, string>();

        private static volatile bool _keepGoing = true;
        private static Task _handleTask;

        private HttpListener _listener;
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
            Debug.Print("Init Begin");
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
                Debug.Print(prefix);
                _listener.Prefixes.Add(prefix);
            });

            if (_users.Count > 0)
                _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;

            Debug.Print("Init End");
        }

        public void Start()
        {
            if (_handleTask != null && !_handleTask.IsCompleted) return; //Already started
            _handleTask = HandleTask();
        }

        private async Task HandleTask()
        {
            Debug.Print("HandleTask Started");
            try
            {
                _listener.Start();
                Debug.Print("HttpServer - started. Will listen on port " + _port);

                while (_keepGoing)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        lock (_listener)
                        {
                            Debug.Print("_listener");
                            if (_keepGoing) OnRequest(context);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("" + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("" + ex);
            }
        }
        public void Stop()
        {
            _keepGoing = false;
            lock (_listener)
            {
                //Use a lock so we don't kill a request that's currently being processed
                _listener.Stop();
            }
            try
            {
                _handleTask.Wait();
            }
            catch { /* je ne care pas */ }
        }

        private void OnRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            Debug.Print("OnRequest: " + request.RemoteEndPoint.Address + " " + request.HttpMethod + " " + request.Url + "");

            if (auth(context) == null)
            {
                response.StatusCode = ((int)HttpStatusCode.Unauthorized);
            }
            else
            {
                String method = request.HttpMethod;
                String path = request.Url.AbsolutePath;

                if (path == "/hello")
                {
                    string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;

                    Stream output = response.OutputStream;
                    StreamWriter writer = new StreamWriter(output);
                    writer.Write(responseString);
                    // You must close the output stream.
                    writer.Close();
                }
                else
                {
                    response.StatusCode = ((int)HttpStatusCode.NotFound);
                }
            }

            response.Close();
        }

        private String auth(HttpListenerContext context)
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

            return null;
        }
    }
}
