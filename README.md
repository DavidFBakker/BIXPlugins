# BIXPlugins
Some random plugins for home automation

#BixLIFXService 
this service (or can be run as a console program) simply controls your LIFX bulbs. It listens on a http port to control the lights. It listens on all IP address on port 9105
The commands it supports are
ListLights=1 - List all the discovered LIFX bulbs
ListColors=1 - List all the supported colors. 
Power=On/Off/Toggle - As it says
Dim=20 - 1-100 to dim a light
Color=<Color> - Sets a color ,This can be kelvin (2500-9000), HTML hex (#RRGGBB) or a name Red/LawnGreen
Light=<Label nam> - The LIFX light name. this can also be ALL or if you dont use a full name it will match (StartOf) any bulb label to that value. So "Office Light" will work on a bulb called "Office Light" or all bulbs starting with "Office Light" in their label

examples:
http://localhost:9105/?Light=Office Light 1&Power=On&Dim=80&Color=Pink - Turn on "Office Light 1", set the DIM to 80% and make the color pink
http://localhost:9105/?Light=Office Light&Power=Toggle - Toggle the light state for all lights starting with "Office Light"
http://localhost:9105/?Light=Office Light&Power=On&Dim=80&Color=2700 - Turn on all "Office Light"s Dim to 80% and make it a white color of kelvin 2700


so the cool thing is you can use any home automation software to simply trigger this. I have Amazon Echo Bridge (https://github.com/armzilla/amazon-echo-ha-bridge)
running with some bindings for the bulbs. This way alexa can control them. I also have homeseer using it with some virtual bulbs.
Bridge setup is simple. Make a manual entry
Name "Office Light 1" - bulb label
Device type TCP
ON URl
http://192.168.1.19:9105/?Light=Office%20Light%201&Power=On
Off URL
http://192.168.1.19:9105/?Light=Office%20Light%201&Power=Off
Dim URL
http://192.168.1.19:9105/?Light=Office%20Light%201&Power=On&Dim=${intensity.percent}

Then i can have alexa power or dim the bulb

Homeseer:
create a dimable virtual device and create an event to pass the state change to a homeseer script. The event executes the script on a value changed with:
Sub: LightParseCommand
Parameters: Office Light 1

Here is the script:
The script (C#):
using System.Net;
using HomeSeerAPI;
using Scheduler;
using Scheduler.Classes;

private const string BaseUrl = @"http://192.168.1.19:9105/?";

public object LightParseCommand(string device)
{
    var dvRef = hs.GetDeviceRefByName(device);
    if (dvRef > 0)
    {
        var status = hs.CAPIGetStatus(dvRef).Status;
        string cmdstr = "";
        if (status.StartsWith("Dim"))
        {
            cmdstr = "Power=On&Dim=" + status.Substring(3);
        }
        else if (status.Equals("On"))
        {
            cmdstr = "Power=On";
        }
        else if (status.Equals("Off"))
        {
            cmdstr = "Power=Off";
        }
        if (!string.IsNullOrEmpty(cmdstr))
        {
            var page = BaseUrl + "Light=" + device + "&" + cmdstr;
            hs.WriteLog("Info", "Sending " + page);
            System.Net.WebClient webClient = new System.Net.WebClient();
            string response = webClient.DownloadString(page);
        }
    }
    return 0;
}
