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
        private const int CommandSends = 1;

        private  static readonly TimeSpan TaskTimeout = new TimeSpan(1000);

       // private static readonly object AddLock = new object();
       //private static readonly object RemoveLock = new object();


        private static LifxClient _client;
        private static readonly ushort KelvinLow = 2500;
        private static readonly ushort KelvinHigh = 9000;

        //private static readonly object Obj = new object();
        //private static readonly object LockObj = new object();
        //  private static ObservableCollection<LightBulb> _bulbs;

        public static ObservableCollection<LightBulb> Bulbs = new ObservableCollection<LightBulb>();
        //{
        //    get
        //    {
        //        lock (LockObj)
        //        {
        //            if (_bulbs == null)
        //                _bulbs = new ObservableCollection<LightBulb>();

        //            return _bulbs;
        //        }
        //    }
        //    //set
        //    //{
        //    //    lock (LockObj)
        //    //    {
        //    //        _bulbs = value;
        //    //    }
        //    //}
        //}

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

        public static string ColorsTable
        {
            get
            {
                var colors = new StringBuilder();
                colors.AppendLine("<table>");
                foreach (var key in BIXColors.Colors.Keys)
                {
                    var bixColor = BIXColors.Colors[key];
                    colors.AppendLine(bixColor.TableRow);
                }
                colors.AppendLine("</table>");
                return colors.ToString();
            }
        }

        public static List<string> ColorNames => BIXColors.Colors.Keys.ToList();

        private static List<string> Labels
        {
            get
            {
                if (Bulbs == null)
                    return new List<string>();

                return
                    Bulbs.Where(a => !IsNullOrEmpty(a.State?.Label))
                        .Select(a => a.State.Label)
                        .ToList();
            }
        }


        private static async void BixListens_OnHttpEventReceived(object sender, HttpEvent e)
        {
           
            Log.Bulb($"{e.ID} Received new event");
            var responseBuilder = new StringBuilder();

            if (!IsNullOrEmpty(e.QueryString["UpdateState"]))
            {
                foreach (var bulb in Bulbs)
                    bulb.State = await _client.GetLightStateAsync(bulb);

                SendResponse(e.HttpListenerResponse, "OK");
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }

            if (!IsNullOrEmpty(e.QueryString["Log"]))
            {
                SendResponse(e.HttpListenerResponse, Log.GetMessages());
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }


            if (!IsNullOrEmpty(e.QueryString["BuildBulbs"]))
            {
                SendResponse(e.HttpListenerResponse, BuildCreateDevice());
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }

            if (!IsNullOrEmpty(e.QueryString["Status"]))
            {
                SendResponse(e.HttpListenerResponse, BuildStatus());
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }

            if (!IsNullOrEmpty(e.QueryString["ListColors"]))
            {
                SendResponse(e.HttpListenerResponse, ColorsTable);
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }

            if (!IsNullOrEmpty(e.QueryString["ListLights"]))
            {
                var lights = Bulbs.Where(a => a.State != null).Select(a => a.State.Label).OrderBy(b1 => b1).ToList();
                foreach (var light1 in lights)
                    responseBuilder.AppendLine(light1);

                SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }


            if (IsNullOrEmpty(e.QueryString["Light"]))
            {
                SendResponse(e.HttpListenerResponse, "Need a light");
                Log.Bulb($"{e.ID} Proccessed event");
                return;
            }

            var light = e.QueryString["Light"];
            var bulbs = new List<LightBulb>();
            if (light == "all")
            {
                lock (Bulbs)
                {
                    bulbs.AddRange(Bulbs);
                }
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
                    lock (new object())
                    {
                        var power = SetPower(bulb, e.ID, powerstate);
                        responseBuilder.AppendLine($"Powered {light} from {powerstate} to {power.Result}");
                    }
                }
            }
            var goodCommand = false;
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
                    goodCommand = true;
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
                var isGood = true;
                colorStr = e.QueryString["Color"].ToLower();
                if (IsNullOrEmpty(colorStr))
                {
                    isGood = false;
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
                        isGood = false;
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
                    isGood = false;
                }


                if (!isGood)
                {
                    SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
                    Log.Bulb($"{e.ID} Proccessed event");
                    return;
                }
                goodCommand = true;
            }
            if (goodCommand)
            {
                foreach (var bulb in bulbs)
                {
                    lock (new object())
                    {
                        var t = SetColor(bulb,e.ID, hue, saturation, brightness, kelvin);
                        responseBuilder.AppendLine(
                            $"Set color for bulb {bulb.State.Label} to color {colorStr} hue: {hue} saturation: {saturation} brightness: {brightness} kelvin: {kelvin}");
                    }
                }
            }
            SendResponse(e.HttpListenerResponse, responseBuilder.ToString());
            Log.Bulb($"{e.ID} Proccessed event");
        }

        private static string FontSize(int size)
        {
            return $"style=\"font-family:arial;font-size:{size}px; \"";
        }

        private static string BuildStatus()
        {
            var ret = new StringBuilder();

            ret.AppendLine("<html>");
            ret.AppendLine("<head>");
            ret.AppendLine("</head>");
            ret.AppendLine("<body>");

            ret.AppendLine("<table border=\"1\">");
            var bulbs = Bulbs.Where(a => !IsNullOrEmpty(a.State?.Label)).OrderBy(a => a.State.Label).ToList();

            ret.AppendLine(
                $"<tr {FontSize(32)}\"><td>Label</td><td>IsOn</td><td>Hue</td><td>Saturation</td><td>Brightness</td><td>Kelvin</td><td>Color</td></tr>");

            foreach (var bulb in bulbs)
            {
                var h = (float) (bulb.State.Hue/65535.0);
                var s = (float) (bulb.State.Saturation/65535.0);
                var b = (float) (bulb.State.Brightness/65535.0);


                var color = HSBtoHEX(h, s, 1f);

                var sb = new StringBuilder();

                sb.AppendLine($"<tr {FontSize(20)}>");
                sb.AppendLine($"<td>{bulb.State.Label}</td>");
                sb.AppendLine($"<td>{bulb.State.IsOn}</td>");
                //   sb.AppendLine($"<td>{bulb.IsOn}</td>");
                sb.AppendLine($"<td>{h:F2}</td>");
                sb.AppendLine($"<td>{s:F2}</td>");
                sb.AppendLine($"<td>{b:F2}</td>");
                sb.AppendLine($"<td>{bulb.State.Kelvin}</td>");
                sb.AppendLine($"<td bgcolor=\"{color}\" width=\"50px\"></td>");
                sb.AppendLine("</tr>");
                ret.AppendLine(sb.ToString());
            }
            ret.AppendLine("</table>");

            ret.AppendLine("</body>");
            ret.AppendLine("</html>");
            return ret.ToString();
        }

        public static string HSBtoHEX(float hue, float saturation, float brightness)
        {
            int r = 0, g = 0, b = 0;
            if (saturation == 0)
            {
                r = g = b = (int) (brightness*255.0f + 0.5f);
            }
            else
            {
                var h = (hue - (float) Math.Floor(hue))*6.0f;
                var f = h - (float) Math.Floor(h);
                var p = brightness*(1.0f - saturation);
                var q = brightness*(1.0f - saturation*f);
                var t = brightness*(1.0f - saturation*(1.0f - f));
                switch ((int) h)
                {
                    case 0:
                        r = (int) (brightness*255.0f + 0.5f);
                        g = (int) (t*255.0f + 0.5f);
                        b = (int) (p*255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int) (q*255.0f + 0.5f);
                        g = (int) (brightness*255.0f + 0.5f);
                        b = (int) (p*255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int) (p*255.0f + 0.5f);
                        g = (int) (brightness*255.0f + 0.5f);
                        b = (int) (t*255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int) (p*255.0f + 0.5f);
                        g = (int) (q*255.0f + 0.5f);
                        b = (int) (brightness*255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int) (t*255.0f + 0.5f);
                        g = (int) (p*255.0f + 0.5f);
                        b = (int) (brightness*255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int) (brightness*255.0f + 0.5f);
                        g = (int) (p*255.0f + 0.5f);
                        b = (int) (q*255.0f + 0.5f);
                        break;
                }
            }

            var hex = $"#{Convert.ToByte(r):X2}{Convert.ToByte(g):X2}{Convert.ToByte(b):X2}";
            return hex;
        }

        public static Color HSBtoRGB(float hue, float saturation, float brightness)
        {
            int r = 0, g = 0, b = 0;
            if (saturation == 0)
            {
                r = g = b = (int) (brightness*255.0f + 0.5f);
            }
            else
            {
                var h = (hue - (float) Math.Floor(hue))*6.0f;
                var f = h - (float) Math.Floor(h);
                var p = brightness*(1.0f - saturation);
                var q = brightness*(1.0f - saturation*f);
                var t = brightness*(1.0f - saturation*(1.0f - f));
                switch ((int) h)
                {
                    case 0:
                        r = (int) (brightness*255.0f + 0.5f);
                        g = (int) (t*255.0f + 0.5f);
                        b = (int) (p*255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int) (q*255.0f + 0.5f);
                        g = (int) (brightness*255.0f + 0.5f);
                        b = (int) (p*255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int) (p*255.0f + 0.5f);
                        g = (int) (brightness*255.0f + 0.5f);
                        b = (int) (t*255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int) (p*255.0f + 0.5f);
                        g = (int) (q*255.0f + 0.5f);
                        b = (int) (brightness*255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int) (t*255.0f + 0.5f);
                        g = (int) (p*255.0f + 0.5f);
                        b = (int) (brightness*255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int) (brightness*255.0f + 0.5f);
                        g = (int) (p*255.0f + 0.5f);
                        b = (int) (q*255.0f + 0.5f);
                        break;
                }
            }
            return Color.FromArgb(Convert.ToByte(255), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        private static string BuildCreateDevice()
        {
            var ret = new StringBuilder();

            ret.AppendLine("<html>");
            ret.AppendLine("<head>");
            ret.AppendLine("</head>");
            ret.AppendLine("<body>");


            ret.AppendLine(@"public object CreateDev(string device)
{
    var dvRef = hs.GetDeviceRefByName(device);
    if (dvRef > 0)
    {
        hs.WriteLog(""Info"", ""Devce exists"" + dvRef + "" deleting"");
        hs.DeleteDevice(dvRef);
        hs.SaveEventsDevices();
    }

    Scheduler.Classes.DeviceClass deviceClass = hs.NewDeviceEx(device);
    dvRef = deviceClass.get_Ref(hs);
    deviceClass.set_Can_Dim(hs, true);
    deviceClass.set_Location(hs, ""LIFX"");
    deviceClass.set_Location2(hs, device);
    deviceClass.set_Code(hs, device);
    deviceClass.set_Device_Type_String(hs, ""LIFX Bulb"");
    deviceClass.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);

    CreatePair(dvRef, ""Dim"");
    CreatePair(dvRef, ""On"");
    CreatePair(dvRef, ""Off"");

   
    var newevent = hs.NewEventEx(device + "" On"", device, ""Event stype"");
    var capiOn = GetCAPI(dvRef, ""On"");

    var newevent = hs.NewEventEx(device + "" On"", device, ""Event stype"");
    hs.AddDeviceActionToEvent(newevent, capiOn);
    hs.AddDeviceActionToEvent(newevent, capiOn);
    hs.EnableEventByRef(newevent);

    hs.WriteLog(""Info"", ""Created device "" + device + "" "" + dvRef);
    hs.SaveEventsDevices();

    return 0;
}

private void CreatePair(int dvRef, string command)
{
    HomeSeerAPI.VSVGPairs.VSPair VSPair = new HomeSeerAPI.VSVGPairs.VSPair(ePairStatusControl.Both);
    HomeSeerAPI.VSVGPairs.VGPair VGPair = new HomeSeerAPI.VSVGPairs.VGPair();


    switch (command.ToLower())
    {
        case ""on"":
            VSPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
            VSPair.Render = Enums.CAPIControlType.Button;

            VGPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
            VGPair.Set_Value = 100;
            VGPair.Graphic = ""/images/HomeSeer/status/on.gif"";

            VSPair.Value = 100;
            VSPair.Status = ""On"";
            VSPair.ControlUse = ePairControlUse._On;
            break;
        case ""off"":
            VSPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
            VSPair.Render = Enums.CAPIControlType.Button;

            VGPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
            VGPair.Set_Value = 0;
            VGPair.Graphic = ""/images/HomeSeer/status/off.gif"";

            VSPair.Value = 0;
            VSPair.Status = ""Off"";
            VSPair.ControlUse = ePairControlUse._Off;
            break;
        case ""dim"":
           
            VSPair.RangeStatusPrefix = ""Dim"";
            VSPair.RangeStart = 1;
            VSPair.RangeEnd = 100;
            VSPair.PairType = VSVGPairs.VSVGPairType.Range;
            VSPair.Render = Enums.CAPIControlType.ValuesRangeSlider;
            VSPair.Status = ""Dim88"";
            VSPair.ControlUse = ePairControlUse._Dim;
            break;
    }

    hs.DeviceVSP_AddPair(dvRef, VSPair);
    if (command.ToLower() != ""dim"")
        hs.DeviceVGP_AddPair(dvRef, VGPair);
}");
            ret.AppendLine(@"public void CreateBulbs()
{ ");

            foreach (var bulb in Bulbs)
            {
                ret.AppendLine(@"    CreateDev(""" + bulb.State.Label + @""");");
            }
            ret.AppendLine("}");

            ret.AppendLine("</body>");
            ret.AppendLine("</html>");
            return ret.ToString();
        }

        private static async Task SetColor(LightBulb bulb, string eventID, ushort hue, ushort saturation, ushort brightness,
            ushort kelvin)
        {
            Log.Bulb($"{eventID} SetColor {bulb.State.Label}");

            //  bulb.State = await _client.GetLightStateAsync(bulb);

            var chue = bulb.State.Hue;
            var csaturation = bulb.State.Saturation;
            var cbrightness = bulb.State.Brightness;
            var ckelvin = bulb.State.Kelvin;

            if (kelvin == ckelvin && hue == chue && saturation == csaturation && brightness == cbrightness)
                return;
            if (kelvin < KelvinLow)
                kelvin = KelvinLow;

            if (kelvin > KelvinHigh)
                kelvin = KelvinHigh;

            for (var count = 0; count < CommandSends; ++count)
            {
                await
                    _client.SetColorAsync(bulb, hue, saturation, brightness, kelvin, new TimeSpan(0)).TimeoutAfter(TaskTimeout); 
                bulb.State.Hue = hue;
                bulb.State.Saturation = saturation;
                bulb.State.Brightness = brightness;
                bulb.State.Kelvin = kelvin;
            }

            Log.Bulb($"{eventID} SetColor {bulb.State.Label} Done");
        }

        private static async Task SetColor(LightBulb bulb, ushort kelvin)
        {
            //bulb.State = await _client.GetLightStateAsync(bulb);

            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin1 = bulb.State.Kelvin;

            if (kelvin == kelvin1 && hue == 255 && saturation == 255)
                return;

            for (var count = 0; count < CommandSends; ++count)
            {
                await
                    _client.SetColorAsync(bulb, 0, 0, brightness, kelvin,
                        new TimeSpan(0)).TimeoutAfter(TaskTimeout);
                bulb.State.Kelvin = kelvin;
            }
        }

        private static async Task SetColor(LightBulb bulb, BIXColor bixColor)
        {
            //   bulb.State = await _client.GetLightStateAsync(bulb);

            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin = bulb.State.Kelvin;

            if (hue == bixColor.LIFXHue && saturation == bixColor.LIFXSaturation)
                return;

            for (var count = 0; count < CommandSends; ++count)
            {
                await
                    _client.SetColorAsync(bulb, bixColor.LIFXHue, bixColor.LIFXSaturation, brightness, kelvin,
                        new TimeSpan(0)).TimeoutAfter(TaskTimeout);
                bulb.State.Hue = bixColor.LIFXHue;
                bulb.State.Saturation = bixColor.LIFXSaturation;
            }
        }

        private static async Task<string> SetPower(LightBulb bulb,string eventid, string powerState)
        {
            Log.Bulb($"{eventid} SetPower {bulb.State.Label} {powerState}");
            //  bulb.State = await _client.GetLightStateAsync(bulb);
            if (powerState == "toggle")
            {
                powerState = bulb.State.IsOn ? "off" : "on";
            }

            for (var count = 0; count < CommandSends; ++count)
            {
                await _client.SetDevicePowerStateAsync(bulb, powerState == "on").TimeoutAfter(TaskTimeout);                
            }

            Log.Bulb($"{eventid} SetPower {bulb.State.Label} {powerState} Done");

            return powerState;
        }

        private static async Task<ushort> Dim(LightBulb bulb, ushort dim)
        {
            //   bulb.State = await _client.GetLightStateAsync(bulb);
            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;
            var brightness = bulb.State.Brightness;
            var kelvin = bulb.State.Kelvin;

            if (brightness != dim)
                for (var count = 0; count < CommandSends; ++count)
                    await _client.SetColorAsync(bulb, hue, saturation, dim, kelvin, new TimeSpan(0)).TimeoutAfter(TaskTimeout);

            return brightness;
        }

        private static void SendResponse(HttpListenerResponse response, string message)
        {
            Log.Bulb($"Responded with {message}");
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);

                response.ContentLength64 = buffer.LongLength;
                var a = response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                a.Wait(200);
            }
            catch (Exception ex)
            {
                Log.Error($"Error on response: {ex.Message}");
            }
            try
            {
                response.Close();
            }
            catch
            {
            }
        }

        public static async Task Init()
        {
            Log.Bulb("Starting BixListens");
            var bixListens = new BixListens();
            bixListens.OnHttpEventReceived += BixListens_OnHttpEventReceived;

            Log.Bulb("Creating LifxClient");
            _client = await LifxClient.CreateAsync();
            _client.DeviceDiscovered += Client_DeviceDiscovered;
            _client.DeviceLost += Client_DeviceLost;
            Log.Bulb("Start Device Discovery");
            _client.StartDeviceDiscovery();
        }

        private static void Client_DeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            lock (Bulbs)
            {
                var bulb = e.Device as LightBulb;

                if (IsNullOrEmpty(bulb?.State?.Label) || !Labels.Contains(bulb.State.Label)) return;

                if (bulb.LastSeen.AddMinutes(10) > DateTime.Now)
                    return;

                Log.Bulb($"Removing bulb {bulb.State.Label}");
                try
                {
                    Bulbs.Remove(bulb);
                }
                catch (Exception ex)
                {
                    Log.Error($"Removing bulb {bulb.State.Label} exception {ex.Message}");
                }
            }
        }

        private static void Client_DeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            lock (Bulbs)
            {
                var bulb = e.Device as LightBulb;

                if (IsNullOrEmpty(bulb?.State?.Label) || Labels.Contains(bulb.State.Label)) return;

                Log.Bulb($"Adding bulb {bulb.State.Label}");

                try
                {
                    Bulbs.Add(bulb);
                }
                catch (Exception ex)
                {
                    Log.Error($"Adding bulb {bulb.State.Label} exception {ex.Message}");
                }
            }
        }
    }
}