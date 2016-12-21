using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace BixPlugins
{
    public class HttpRequestHandler
    {
        private readonly ManualResetEvent stopEvent = new ManualResetEvent(false);
     //   private int requestCounter;

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
                    Log.Error("Listener stopped on event signal");
                    break;
                }
            }

            Log.Error("Uh Oh Listener stopped IsListening");
        }

        private void ListenerCallback(IAsyncResult ar)
        {
            var callbackState = (HttpListenerCallbackState) ar.AsyncState;
            HttpListenerContext context = null;

            //var requestNumber = Interlocked.Increment(ref requestCounter);

            try
            {
                context = callbackState.Listener.EndGetContext(ar);
            }
            catch (Exception ex)
            {
                Log.Error($"ListenerCallback error {ex.Message}");
                return;
            }
            finally
            {
                callbackState.ListenForNextRequest.Set();
            }


            var request = context.Request;


            //if (request.HasEntityBody)
            //{
            //    using (var sr = new StreamReader(request.InputStream, request.ContentEncoding))
            //    {
            //        var requestData = sr.ReadToEnd();

            //        //Stuff I do with the request happens here  
            //    }
            //}

            //if (request.QueryString.HasKeys())
            //    foreach (var q in request.QueryString)
            //    {
            //        var value = request.QueryString[q.ToString()];
            //        if (value == null)
            //            value = "";
            //        Log.Info($"Query String {q}:value");
            //    }

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
                 
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ListenerCallback error {ex.Message}");
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