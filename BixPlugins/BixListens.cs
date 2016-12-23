using System;
using System.IO;
using System.Net;
using System.Threading;

namespace BixPlugins
{
  
    public class BixListens
    {        
        public BixListens(int port = 9105)
        {
            //var handler = new HttpServer(8);
            //handler.ProcessRequest += Handler_ProcessRequest; ;
            //handler.Start(port);

            var handler = new HttpRequestHandler();
            handler.OnHttpEventReceived += Handler_OnHttpEventReceived;
            handler.ListenAsynchronously($"http://+:{port}/");

            //var handler = new HTTTPListener();
            //handler.OnHttpEventReceived += Handler_OnHttpEventReceived;
            //handler.Listen($"http://+:{port}/", 20, new CancellationToken());

            Log.Info("Listening..");
        }

        private void Handler_OnHttpEventReceived(object sender, HttpEvent e)
        {
            OnHttpEventReceived?.Invoke(this, e);
        }

        private void Handler_ProcessRequest(HttpEvent e)
        {
            OnHttpEventReceived?.Invoke(this, e);
        }

        public event HttpEventHandler OnHttpEventReceived;

        protected virtual void OnOnHttpEventCReceived(HttpEvent e)
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
}