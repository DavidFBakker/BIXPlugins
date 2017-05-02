# BIXPlugins
Some random plugins for home automation

## BixLIFXConsole
this console app simply controls your LIFX bulbs. It listens on a http port to control the lights. It listens on all IP address on port 9105
The commands it supports are

- **ListLights=1 -** List all the discovered LIFX bulbs
- **ListColors=1 -** List all the supported colors. 
- **Power=On/Off/Toggle** - As it says
- **Dim=20 -** 1-100 to dim a light
- **Color=<Color> -** Sets a color ,This can be kelvin (2500-9000), HTML hex (#RRGGBB) or a name Red/LawnGreen
- **Light=<Label nam> -** The LIFX light name. this can also be ALL or if you dont use a full name it will match (StartOf) any bulb label to that value. So "Office Light" will work on a bulb called "Office Light" or all bulbs starting with "Office Light" in their label
- **UpdateState=1 -** Force a state update (useful if the bulbs were modified outside of this program)
- **Log=1 -** Show the last 100 log entries
- **Status=1 -** Show the status of all the bulbs
- **UpdateState=1 -** Update the state of all the bulbs

#### Examples:
**http://localhost:9105/?Light=Office Light 1&Power=On&Dim=80&Color=Pink** - Turn on "Office Light 1", set the DIM to 80% and make the color pink**
**http://localhost:9105/?Light=Office Light&Power=Toggle** - Toggle the light state for all lights starting with "Office Light"**
**http://localhost:9105/?Light=Office Light&Power=On&Dim=80&Color=2700** - Turn on all "Office Light"s Dim to 80% and make it a white color of kelvin 2700**


So the cool thing is you can use any home automation software to simply trigger this. I have Amazon Echo Bridge (https://github.com/armzilla/amazon-echo-ha-bridge)
running with some bindings for the bulbs. This way alexa can control them. I also have homeseer using it with some virtual bulbs.
#### Bridge setup is simple. Make a manual entry
- Name "Office Light 1" - bulb label
- Device type TCP

On URL
**http://192.168.1.19:9105/?Light=Office%20Light%201&Power=On**

Off URL
**http://192.168.1.19:9105/?Light=Office%20Light%201&Power=Off**

Dim URL
http://192.168.1.19:9105/?Light=Office%20Light%201&Power=On&Dim=${intensity.percent}

Then i can have alexa power or dim the bulb

Homeseer:
create a dimable virtual device and create an event to pass the state change to a homeseer script. The event executes the script on a value changed with:
- Sub: LightParseCommand
- Parameters: Office Light 1

Here is the script:
The script (C#):
```
using System.Net;
using HomeSeerAPI;
using Scheduler;
using Scheduler.Classes;

private const string BaseUrl = @"http://192.168.1.19:9105/?";

public object LightCommand(string command)
{
    var page = BaseUrl + command;
    hs.WriteLog("Info", "Sending " + page);

    System.Net.WebClient webClient = new System.Net.WebClient();
    string response = webClient.DownloadString(page);
    webClient.Dispose();


    return 0;
}
```


## TPLinkConsole
Contorl your TP wall plugs and light switches

### Example

TPLinkConsole.exe <ip of device> ON

