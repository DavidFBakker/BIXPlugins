using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;

namespace LifxNet
{
    public partial class LifxClient : IDisposable
    {
        private static readonly Random randomizer = new Random();
        private CancellationTokenSource _DiscoverCancellationSource;

        private readonly IList<Device> devices = new List<Device>();
        private readonly Dictionary<string, Device> DiscoveredBulbs = new Dictionary<string, Device>();
        private uint discoverSourceID;

        /// <summary>
        ///     Gets a list of currently known devices
        /// </summary>
        public IEnumerable<Device> Devices => devices;

        /// <summary>
        ///     Event fired when a LIFX bulb is discovered on the network
        /// </summary>
        public event EventHandler<DeviceDiscoveryEventArgs> DeviceDiscovered;

        /// <summary>
        ///     Event fired when a LIFX bulb hasn't been seen on the network for a while (for more than 5 minutes)
        /// </summary>
        public event EventHandler<DeviceDiscoveryEventArgs> DeviceLost;

        private async void ProcessDeviceDiscoveryMessage(HostName remoteAddress, string remotePort, LifxResponse msg)
        {
            if (DiscoveredBulbs.ContainsKey(remoteAddress.ToString())) //already discovered
            {
                DiscoveredBulbs[remoteAddress.ToString()].LastSeen = DateTime.UtcNow; //Update datestamp
                return;
            }
            if (msg.Source != discoverSourceID || //did we request the discovery?
                _DiscoverCancellationSource == null ||
                _DiscoverCancellationSource.IsCancellationRequested) //did we cancel discovery?
                return;

            var device = new LightBulb
            {
                HostName = remoteAddress,
                Service = msg.Payload[0],
                Port = BitConverter.ToUInt32(msg.Payload, 1),
                LastSeen = DateTime.UtcNow
            };
            device.State = await GetLightStateAsync(device);

            DiscoveredBulbs[remoteAddress.ToString()] = device;

            device.State = await GetLightStateAsync(device);
            // device.IsOn = await GetLightPowerAsync(device);

            devices.Add(device);
            if (DeviceDiscovered != null)
                DeviceDiscovered(this, new DeviceDiscoveryEventArgs {Device = device});
        }

        /// <summary>
        ///     Begins searching for bulbs.
        /// </summary>
        /// <seealso cref="DeviceDiscovered" />
        /// <seealso cref="DeviceLost" />
        /// <seealso cref="StopDeviceDiscovery" />
        public void StartDeviceDiscovery()
        {
            if (_DiscoverCancellationSource != null && !_DiscoverCancellationSource.IsCancellationRequested)
                return;
            _DiscoverCancellationSource = new CancellationTokenSource();
            var token = _DiscoverCancellationSource.Token;
            var source = discoverSourceID = (uint) randomizer.Next(int.MaxValue);
            //Start discovery thread
            Task.Run(async () =>
            {
                Debug.WriteLine("Sending GetServices");
                var header = new FrameHeader
                {
                    Identifier = source
                };
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await BroadcastMessageAsync<UnknownResponse>(null, header, MessageType.DeviceGetService, null);
                    }
                    catch
                    {
                    }
                    await Task.Delay(5000);
                    var lostDevices = devices.Where(d => (DateTime.UtcNow - d.LastSeen).TotalMinutes > 5).ToArray();
                    if (lostDevices.Any())
                        foreach (var device in lostDevices)
                        {
                            devices.Remove(device);
                            DiscoveredBulbs.Remove(device.HostName.ToString());
                            if (DeviceLost != null)
                                DeviceLost(this, new DeviceDiscoveryEventArgs {Device = device});
                        }
                }
            });
        }

        /// <summary>
        ///     Stops device discovery
        /// </summary>
        /// <seealso cref="StartDeviceDiscovery" />
        public void StopDeviceDiscovery()
        {
            if (_DiscoverCancellationSource == null || _DiscoverCancellationSource.IsCancellationRequested)
                return;
            _DiscoverCancellationSource.Cancel();
            _DiscoverCancellationSource = null;
        }

        /// <summary>
        ///     Event args for <see cref="DeviceDiscovered" /> and <see cref="DeviceLost" /> events.
        /// </summary>
        public sealed class DeviceDiscoveryEventArgs : EventArgs
        {
            /// <summary>
            ///     The device the event relates to
            /// </summary>
            public Device Device { get; internal set; }
        }
    }

    /// <summary>
    ///     LIFX Generic Device
    /// </summary>
    public abstract class Device
    {
        internal Device()
        {
        }

        /// <summary>
        ///     Hostname for the device
        /// </summary>
        public HostName HostName { get; internal set; }

        public string HostNameString => HostName.ToString();

        /// <summary>
        ///     Service ID
        /// </summary>
        public byte Service { get; internal set; }

        /// <summary>
        ///     Service port
        /// </summary>
        public uint Port { get; internal set; }

        public bool IsOn { get; set; }

        public LightStateResponse State { get; set; }
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    ///     LIFX light bulb
    /// </summary>
    public sealed class LightBulb : Device
    {
        internal LightBulb()
        {
        }
    }
}