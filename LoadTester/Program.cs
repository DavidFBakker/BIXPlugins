﻿using System;
using System.Net;
using System.Threading;
using BixPlugins;
using BixPlugins.BixLIFX;

namespace LoadTester
{
    internal class Program
    {
        private  static string BaseUrl = @"http://192.168.1.19:9105/?";
        private static Random _random;

        private static void Main(string[] args)
        {
            // BixLIFX.Init();
            BaseUrl = @"http://127.0.0.1:9105/?";
            var bulb = "Office Light 1";
            var speed = .4*1024;
            _random = new Random();
            var errors = 0;
            var runs = 0;
          //  bulb = "Kitchen Cook";
            //foreach (var bulb in BixLIFX.Bulbs)
            while (true)

            {
                var d = _random.Next(20, 100);
                var r = _random.Next(2, BixLIFX.ColorNames.Count);
                var colorRandom = BixLIFX.ColorNames[r];

                //var power = "Off";
                //if (r%2 == 0)
                //    power = "On";
                var power = "On";

                var cmd = $"Light={bulb}&Power={power}&Color={colorRandom}&Dim={d}";

               // Console.WriteLine($"{bulb} {power} {d} {colorRandom} {runs++}/{errors}");
                Log.Bulb($"{bulb} {power} {d} {colorRandom} {runs++}/{errors}");
                var page = $"{BaseUrl}{cmd}";
              //  Console.WriteLine(page);
                try
                {
                    //var webClient = new WebDownload();
                    //webClient.Timeout = (int) speed;                  
                    //var response = webClient.DownloadString(page);
                    System.Net.WebClient webClient = new System.Net.WebClient();
                    string response = webClient.DownloadString(page);
                    webClient.Dispose();
                }
                catch (Exception ex)
                {
                //    if ( !ex.Message.Contains("The operation has timed out"))
                     errors++;
                }
                Thread.Sleep((int) (speed));
            }
        }
       
    }
    //public class WebDownload : WebClient
    //{
    //    /// <summary>
    //    /// Time in milliseconds
    //    /// </summary>
    //    public int Timeout { get; set; }

    //    public WebDownload() : this(60000) { }

    //    public WebDownload(int timeout)
    //    {
    //        this.Timeout = timeout;
    //    }

    //    protected override WebRequest GetWebRequest(Uri address)
    //    {
    //        var request = base.GetWebRequest(address);
    //        if (request != null)
    //        {
    //            request.Timeout = this.Timeout;
    //        }
    //        return request;
    //    }
    //}
}