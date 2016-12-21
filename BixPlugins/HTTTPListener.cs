using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BixPlugins
{
    public class HTTTPListener
    {
        public event HttpEventHandler OnHttpEventReceived;

        private void OnOnHttpEventReceived(HttpEvent e)
        {
            OnHttpEventReceived?.Invoke(null, e);
        }


        public async Task Listen(string prefix, int maxConcurrentRequests, CancellationToken token)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var requests = new HashSet<Task>();
            for (var i = 0; i < maxConcurrentRequests; i++)
                requests.Add(listener.GetContextAsync());

            while (!token.IsCancellationRequested)
            {
                var t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>)
                {
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(ProcessRequestAsync(context));
                    requests.Add(listener.GetContextAsync());
                }
                else
                {
                    t.Wait();
                }
            }
        }

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
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
    }
}