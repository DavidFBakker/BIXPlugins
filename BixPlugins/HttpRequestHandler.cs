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
                Log.Info($"Listen Start WaitAny");
                callbackState.Listener.BeginGetContext(ListenerCallback, callbackState);
                var n = WaitHandle.WaitAny(new WaitHandle[] {callbackState.ListenForNextRequest, stopEvent});
                Log.Info($"Listen got event");
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
            var eventID = Guid.NewGuid().ToString();
            Log.Info($"{eventID} Handling event");
            var callbackState = (HttpListenerCallbackState) ar.AsyncState;
            HttpListenerContext context = null;

            //var requestNumber = Interlocked.Increment(ref requestCounter);

            try
            {
                context = callbackState.Listener.EndGetContext(ar);
            }
            catch (Exception ex)
            {
                Log.Error($"{eventID} ListenerCallback error {ex.Message}");
                return;
            }
            finally
            {
                callbackState.ListenForNextRequest.Set();
            }


            var request = context.Request;


           
            try
            {
                using (var response = context.Response)
                {
                    if (request.QueryString.HasKeys())
                    {

                        OnOnHttpEventReceived(new HttpEvent
                        {
                            QueryString = request.QueryString,
                            HttpListenerResponse = response,ID=eventID
                        });
                    }
                 
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{eventID} ListenerCallback error {ex.Message}");
            }
            Log.Info($"{eventID} Handled event");
        }

        public void ListenAsynchronously(string prefix)
        {
            ListenAsynchronously(new List<string> {prefix});
        }

        private void OnOnHttpEventReceived(HttpEvent e)
        {
            Log.Info($"{e.ID} HttpRequestHandler OnHttpEventReceived Start");
            OnHttpEventReceived?.Invoke(null, e);
            Log.Info($"{e.ID} HttpRequestHandler OnHttpEventReceived Ended");
        }
    }
}