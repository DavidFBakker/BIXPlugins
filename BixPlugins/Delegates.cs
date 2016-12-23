using System;
using System.Collections.Specialized;
using System.Net;

namespace BixPlugins
{
    public delegate void MessageEventHandler(object sender, MessageEvent e);

    public class MessageEvent : EventArgs
    {
        public string Message { get; set; }
    }

    public delegate void HttpEventHandler(object sender, HttpEvent e);

    public class HttpEvent : EventArgs
    {
        public string ID { get; set; }
        public NameValueCollection QueryString { get; set; }
        public HttpListenerResponse HttpListenerResponse { get; set; }
    }
}