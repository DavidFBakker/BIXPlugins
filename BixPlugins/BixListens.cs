using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace BixPlugins
{
    public class BixListens
    {
        private HttpListener listener;

        public BixListens(int port = 9105)
        {
            //listener = new HttpListener();
            //listener.Prefixes.Add($"http://+:{port}/");
            //listener.Start();
            var handler = new HttpRequestHandler();
            handler.OnHttpEventReceived += Handler_OnHttpEventReceived;
            handler.ListenAsynchronously($"http://+:{port}/");
            
            Log.Info("Listening..");
        }

        public event HttpEventHandler OnHttpEventReceived;

        private void Handler_OnHttpEventReceived(object sender, HttpEvent e)
        {
            OnOnHttpEventReceived(e);
        }


        protected virtual void OnOnHttpEventReceived(HttpEvent e)
        {
            OnHttpEventReceived?.Invoke(this, e);
        }
    }

    public class HttpListenerCallbackState
    {
        public HttpListenerCallbackState(HttpListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");
            Listener = listener;
            ListenForNextRequest = new AutoResetEvent(false);
        }

        public HttpListener Listener { get; }
        public AutoResetEvent ListenForNextRequest { get; }
    }

    public class HttpRequestHandler
    {
        private readonly ManualResetEvent stopEvent = new ManualResetEvent(false);
        private int requestCounter;

        public event HttpEventHandler OnHttpEventReceived;

        public void ListenAsynchronously(IEnumerable<string> prefixes)
        {
            var listener = new HttpListener();

            foreach (var s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            listener.Start();
            var state = new HttpListenerCallbackState(listener);
            ThreadPool.QueueUserWorkItem(Listen, state);
        }

        public void StopListening()
        {
            stopEvent.Set();
        }


        private void Listen(object state)
        {
            var callbackState = (HttpListenerCallbackState) state;

            while (callbackState.Listener.IsListening)
            {
                callbackState.Listener.BeginGetContext(ListenerCallback, callbackState);
                var n = WaitHandle.WaitAny(new WaitHandle[] {callbackState.ListenForNextRequest, stopEvent});

                if (n == 1)
                {
                    // stopEvent was signalled 
                    callbackState.Listener.Stop();
                    break;
                }
            }
        }

        private void ListenerCallback(IAsyncResult ar)
        {
            var callbackState = (HttpListenerCallbackState) ar.AsyncState;
            HttpListenerContext context = null;

            var requestNumber = Interlocked.Increment(ref requestCounter);

            try
            {
                context = callbackState.Listener.EndGetContext(ar);
            }
            catch (Exception ex)
            {
                return;
            }
            finally
            {
                callbackState.ListenForNextRequest.Set();
            }

            if (context == null) return;


            var request = context.Request;


            if (request.HasEntityBody)
            {
                using (var sr = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var requestData = sr.ReadToEnd();

                    //Stuff I do with the request happens here  
                }
            }

            if (request.QueryString.HasKeys())
                foreach (var q in request.QueryString)
                {
                    var value = request.QueryString[q.ToString()];
                    if (value == null)
                        value = "";
                    Log.Info($"Query String {q}:value");
                }

            try
            {
                using (var response = context.Response)
                {
                    if (request.QueryString.HasKeys())
                    {
                        OnOnHttpEventReceived(new HttpEvent
                        {
                            QueryString = request.QueryString,
                            HttpListenerResponse = response
                        });
                    }
                    //var responseString = "Ok";

                    ////response stuff happens here  


                    //var buffer = Encoding.UTF8.GetBytes(responseString);
                    //response.ContentLength64 = buffer.LongLength;
                    //response.OutputStream.Write(buffer, 0, buffer.Length);
                    //response.Close();
                }
            }
            catch (Exception e)
            {
            }
        }

        public void ListenAsynchronously(string prefix)
        {
            ListenAsynchronously(new List<string> {prefix});
        }

        private void OnOnHttpEventReceived(HttpEvent e)
        {
            OnHttpEventReceived?.Invoke(null, e);
        }
    }
}