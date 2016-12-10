using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HSControl
{
    class Program
    {
        private static Example rootObject;
        private const string BaseUrl = @"http://192.168.1.19/";
        static int Main(string[] args)
        {
           
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("<list> or <event ref/device ref> <command>");
                return 5;
            }

         
            if (args[0].ToLower().Equals("list"))
            {
                GetDevices();
                Console.WriteLine(sb.ToString());
                return 0;
            }

         var   page = $"{BaseUrl}JSON?request=controldevicebylabel&ref={args[0]}&label={args[1]}";
           Console.WriteLine($"Page: {page}");
            try
            {
                var webClient = new WebDownload { Timeout = (int)100 };
                var response = webClient.DownloadString(page);
            }
            catch (Exception ex)
            {
                //    if ( !ex.Message.Contains("The operation has timed out"))            
            }


            return 0;
        }

        static StringBuilder sb = new StringBuilder();
        private static void GetDevices()
        {
            var page = $"{BaseUrl}JSON?request=getcontrol";
            try
            {
                
                var webClient = new WebDownload { Timeout = (int)200 };
                var response = webClient.DownloadString(page);
                rootObject = JsonConvert.DeserializeObject<Example>(response);
                foreach (var device in rootObject.Devices.OrderBy(a => a.name))
                {
                    var labels = device.ControlPairs.Select(a => a.Label).ToList();
                    foreach (var label in labels)
                    {
                        sb.AppendLine($"Device: {device.name} Ref:{device.@ref} Label:{label}");
                       // Console.WriteLine(label);
                    }
                    //sb.AppendLine($"{device.name} {device.@ref}");
                }
              
            }
            catch (Exception ex)
            {
                //    if ( !ex.Message.Contains("The operation has timed out"))            
            }

        }
    }

    public class Range
    {
        public int RangeStart { get; set; }
        public int RangeEnd { get; set; }
        public int RangeStatusDecimals { get; set; }
        public int RangeStatusValueOffset { get; set; }
        public int RangeStatusDivisor { get; set; }
        public string ScaleReplace { get; set; }
        public bool HasScale { get; set; }
        public string RangeStatusPrefix { get; set; }
        public string RangeStatusSuffix { get; set; }
    }

    public class ControlLocation
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int ColumnSpan { get; set; }
    }

    public class ControlPair
    {
        public bool Do_Update { get; set; }
        public bool SingleRangeEntry { get; set; }
        public int ControlButtonType { get; set; }
        public string ControlButtonCustom { get; set; }
        public int CCIndex { get; set; }
        public Range Range { get; set; }
        public int Ref { get; set; }
        public string Label { get; set; }
        public int ControlType { get; set; }
        public ControlLocation ControlLocation { get; set; }
        public int ControlLoc_Row { get; set; }
        public int ControlLoc_Column { get; set; }
        public int ControlLoc_ColumnSpan { get; set; }
        public int ControlUse { get; set; }
        public int ControlValue { get; set; }
        public string ControlString { get; set; }
        public object ControlStringList { get; set; }
        public object ControlStringSelected { get; set; }
        public bool ControlFlag { get; set; }
    }

    public class Device
    {
        public IList<ControlPair> ControlPairs { get; set; }
        public int @ref { get; set; }
    public string name { get; set; }
    public string location { get; set; }
    public string location2 { get; set; }
}

public class Example
{
    public string Name { get; set; }
    public string Version { get; set; }
    public IList<Device> Devices { get; set; }
}


}

