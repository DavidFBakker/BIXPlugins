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
            var handler = new HttpServer(8);
            handler.ProcessRequest += Handler_ProcessRequest;         
            handler.Start(port);            
            Log.Info("Listening..");
        }

        private void Handler_ProcessRequest(HttpEvent e)
        {       
            OnOnHttpEventReceived(e);
        }

        public event HttpEventHandler OnHttpEventReceived;

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
}