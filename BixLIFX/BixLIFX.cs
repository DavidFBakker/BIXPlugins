using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LifxNet;
using static System.String;

namespace BixPlugins.BixLIFX
{
    public static class BixLIFX
    {
        private const int CommandSends = 2;
        public static readonly ObservableCollection<LightBulb> Bulbs = new ObservableCollection<LightBulb>();
        private static LifxClient _client;
        private static int KelvinLow = 2500;
        private static int KelvinHigh = 9000;

        public static string ColorsString
        {
            get
            {
                var colors = new StringBuilder();
                colors.AppendLine("#RRGGBB");
                colors.AppendLine("2500 - 9000 (kelvin)");
                foreach (var key in BIXColors.Colors.Keys)
                {
                    var bixColor = BIXColors.Colors[key];
                    colors.AppendLine(
                        $"{bixColor.Name}:{bixColor.Hue},{bixColor.Saturation},{bixColor.Brightness}:{bixColor.Hex}");
                }
                return colors.ToString();
            }
        }

        private static void BixListens_OnHttpEventReceived(object sender, HttpEvent e)
        {
            var responseBuilder = new StringBuilder();

            if (!IsNullOrEmpty(e.QueryString["ListColors"]))
            {
                SendResponse(e.HttpListenerResponse, ColorsString);
                return;
            }

            if (!IsNullOrEmpty(e.QueryString["ListLights"]))
            {
                var lights = Bulbs.Where(a => a.State != null).Select(a => a.State.Label).OrderBy(b1 => b1).ToList();
                foreach (var light1 in lights)
                    responseBuilder.AppendLine(light1);

                SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
                return;
            }


            if (IsNullOrEmpty(e.QueryString["Light"]))
            {
                SendResponse(e.HttpListenerResponse, "Need a light");
                return;
            }

            var light = e.QueryString["Light"];
            var bulbs = new List<LightBulb>();
            if (light == "all")
            {
                bulbs.AddRange(Bulbs);
            }
            else
            {
                var b = Bulbs.FirstOrDefault(a => a.State != null && a.State.Label == light);
                if (b != null)
                {
                    bulbs.Add(b);
                }
                else
                {
                    var bd = Bulbs.Where(a => a.State != null && a.State.Label.StartsWith(light)).ToList();
                    if (!bd.Any())
                    {
                        SendResponse(e.HttpListenerResponse, $"Cant find light or starts with {light}");
                        return;
                    }
                    bulbs.AddRange(bd);
                }
            }
            //var t1 =UpdateBulbs(bulbs);

            //PowerCommand
            if (!IsNullOrEmpty(e.QueryString["Power"]))
            {
                var powerstate = e.QueryString["Power"].ToLower();
                foreach (var bulb in bulbs)
                {
                    var power = SetPower(bulb, powerstate);


                    responseBuilder.AppendLine($"Powered {light} from {powerstate} to {power.Result}");
                }
            }
            bool GoodCommand = false;
            //Set Color
            ushort hue = 0;
            ushort saturation = 0;
            ushort brightness = 0;
            ushort kelvin = 0;

            if (!IsNullOrEmpty(e.QueryString["Dim"]))
            {
                var dimStr = e.QueryString["Dim"].ToLower();
                ushort dim;
                if (ushort.TryParse(dimStr, out dim))
                {
                    GoodCommand = true;
                    dim = (ushort) (65535*(dim/100.0));
                    brightness = dim;                   
                }
                else
                {
                    responseBuilder.AppendLine($"Dim {dimStr} needs to be a whole number");
                }
            }

            var colorStr = "";
            if (!IsNullOrEmpty(e.QueryString["Color"]))
            {
                var IsGood = true;
                colorStr = e.QueryString["Color"].ToLower();
                if (IsNullOrEmpty(colorStr))
                {
                    IsGood = false;
                    responseBuilder.AppendLine("Color is empty");
                }
                else if (colorStr.StartsWith("#") && colorStr.Length == 7)
                {
                    var color = ColorTranslator.FromHtml(colorStr);
                    colorStr = color.Name;
                }
                else if (colorStr.Length == 4 && ushort.TryParse(colorStr, out kelvin))
                {
                    if (kelvin < 2500 || kelvin > 9000)
                    {
                        responseBuilder.AppendLine($"Kelvin {kelvin} out of range (2500-9000)");
                        IsGood = false;
                    }
                    else
                    {
                        hue = 255;
                        saturation = 255;
                    }
                }
                else if (BIXColors.Colors.ContainsKey(colorStr))
                {
                    var color = BIXColors.Colors[colorStr];
                    hue = color.LIFXHue;
                    saturation = color.LIFXSaturation;
                }
                else
                {
                    responseBuilder.AppendLine($"Cannot find color {colorStr}");
                    IsGood = false;
                }


                if (!IsGood)
                {
                    SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
                    return;
                }
                GoodCommand = true;
            }
            if (GoodCommand)
            {
                foreach (var bulb in bulbs)
                {
                    var t = SetColor(bulb, hue, saturation, brightness, kelvin);
                    responseBuilder.AppendLine(
                        $"Set color for bulb {bulb.State.Label} to color {colorStr} hue: {hue} saturation: {saturation} brightness: {brightness} kelvin: {kelvin}");
                }
            }
            SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
        }

        private static async Task SetColor(LightBulb bulb, ushort hue, ushort saturation, ushort brightness,
            ushort kelvin)
        {
            bulb.State = await _client.GetLightStateAsync(bulb);

            var chue = bulb.State.Hue;
            var csaturation = bulb.State.Saturation;
            var cbrightness = bulb.State.Brightness;
            var ckelvin = bulb.State.Kelvin;

            if (kelvin == ckelvin && hue == chue && saturation == csaturation && brightness == cbrightness)
                return;

            for (var count = 0; count < CommandSends; ++count)
                await
                    _client.SetColorAsync(bulb, hue, saturation, brightness, kelvin, new TimeSpan(0));
        }

        private static async Task SetColor(LightBulb bulb, ushort kelvin)
        {
            bulb.State = await _client.GetLightStateAsync(bulb);

            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin1 = bulb.State.Kelvin;

            if (kelvin == kelvin1 && hue == 255 && saturation == 255)
                return;

            for (var count = 0; count < CommandSends; ++count)
                await
                    _client.SetColorAsync(bulb, 0, 0, brightness, kelvin,
                        new TimeSpan(0));
        }

        private static async Task SetColor(LightBulb bulb, BIXColor bixColor)
        {
            bulb.State = await _client.GetLightStateAsync(bulb);

            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin = bulb.State.Kelvin;

            if (hue == bixColor.LIFXHue && saturation == bixColor.LIFXSaturation)
                return;

            for (var count = 0; count < CommandSends; ++count)
                await
                    _client.SetColorAsync(bulb, bixColor.LIFXHue, bixColor.LIFXSaturation, brightness, kelvin,
                        new TimeSpan(0));
        }

        private static async Task<string> SetPower(LightBulb bulb, string powerState)
        {
            bulb.State = await _client.GetLightStateAsync(bulb);
            if (powerState == "toggle")
            {
                powerState = bulb.State.IsOn ? "off" : "on";
            }

            for (var count = 0; count < CommandSends; ++count)
                await _client.SetDevicePowerStateAsync(bulb, powerState == "on");

            return bulb.State.IsOn ? "on" : "off";
        }

        private static async Task<ushort> Dim(LightBulb bulb, ushort dim)
        {
            bulb.State = await _client.GetLightStateAsync(bulb);
            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin = bulb.State.Kelvin;
            if (brightness != dim)
                for (var count = 0; count < CommandSends; ++count)
                    await _client.SetColorAsync(bulb, hue, saturation, dim, kelvin, new TimeSpan(0));

            return brightness;
        }

        private static void SendResponse(HttpListenerResponse response, string message)
        {
            Log.Info(message);
            var buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.LongLength;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        public static async Task Init()
        {
            var bixListens = new BixListens();
            bixListens.OnHttpEventReceived += BixListens_OnHttpEventReceived;

            _client = await LifxClient.CreateAsync();
            _client.DeviceDiscovered += Client_DeviceDiscovered;
            _client.DeviceLost += Client_DeviceLost;
            _client.StartDeviceDiscovery();
        }


        private static void Client_DeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            var bulb = e.Device as LightBulb;
            if (bulb == null || !Bulbs.Contains(bulb)) return;

            Log.Bulb($"Removing bulb {bulb.State.Label}");
            Bulbs.Remove(bulb);
        }

        private static void Client_DeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            var bulb = e.Device as LightBulb;
            if (bulb == null || Bulbs.Contains(bulb)) return;

            Log.Bulb($"Adding bulb {bulb.State.Label}");
            Bulbs.Add(bulb);
        }
    }
}