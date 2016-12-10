using System;
using System.Net;
using System.Threading;

namespace BixLifxCommand
{
    class Program
    {
        private const string BaseUrl = @"http://192.168.1.19:9105/?";

        static void Main(string[] args)
        {
            var page = $"{BaseUrl}{args[0]}";          
            try
            {
                var webClient = new WebDownload {Timeout = (int) 100};
                var response = webClient.DownloadString(page);
            }
            catch (Exception ex)
            {
                //    if ( !ex.Message.Contains("The operation has timed out"))            
            }
        }
    }
    public class WebDownload : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebDownload() : this(60000) { }

        public WebDownload(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}
