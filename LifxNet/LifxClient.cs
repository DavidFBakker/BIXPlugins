﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace LifxNet
{
    /// <summary>
    ///     LIFX Client for communicating with bulbs
    /// </summary>
    public partial class LifxClient : IDisposable
    {
        private const string Port = "56700";
        private DatagramSocket _socket;
        private IList<string> hostNames;

        private LifxClient()
        {
        }

        /// <summary>
        ///     Disposes the client
        /// </summary>
        public void Dispose()
        {
            _socket.Dispose();
        }

        /// <summary>
        ///     Creates a new LIFX client.
        /// </summary>
        /// <returns>client</returns>
        public static async Task<LifxClient> CreateAsync()
        {
            var client = new LifxClient();
            await client.InitializeAsync().ConfigureAwait(false);
            return client;
        }

        private async Task InitializeAsync()
        {
            //NetworkInformation.GetConnectionProfiles().Select(ni=>ni.NetworkAdapter.)
            var connectionProfile = NetworkInformation.GetConnectionProfiles()
                .LastOrDefault(
                    p => p.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None);

            //using (var stream = await _socket.GetOutputStreamAsync(new HostName(IpProvider.BroadcastAddress), Port))
            hostNames = NetworkInformation.GetHostNames().Select(t => t.ToString()).ToList();
            _socket = new DatagramSocket();
            _socket.Control.DontFragment = true;
            _socket.MessageReceived += HandleIncomingMessages;
            //foreach (var adapter in NetworkInformation.GetConnectionProfiles().Select(t=>t.NetworkAdapter).Distinct())
            //{
            //	await _socket.BindServiceNameAsync(Port, adapter); //, connectionProfile.NetworkAdapter);
            //}
            await _socket.BindServiceNameAsync(Port, connectionProfile.NetworkAdapter);
        }

        private void HandleIncomingMessages(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs e)
        {
            //(await e.RemoteAddress.IPInformation.NetworkAdapter.GetConnectedProfileAsync()).GetNetworkNames();
            try
            {
                if (hostNames != null && hostNames.Contains(e.RemoteAddress.ToString()))
                    return;
                var remote = e.RemoteAddress;
                var local = e.LocalAddress;
                var reader = e.GetDataReader();
                var data = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(data);
                var msg = ParseMessage(data);
                if (msg != null)
                    if (msg.Type == MessageType.DeviceStateService)
                    {
                        ProcessDeviceDiscoveryMessage(e.RemoteAddress, e.RemotePort, msg);
                    }
                    else
                    {
                        if (taskCompletions.ContainsKey(msg.Source))
                        {
                            var tcs = taskCompletions[msg.Source];
                            tcs(msg);
                        }

                        //else if (msg.Type.ToString().StartsWith("State"))
                        //{
                        //}
                        //else if (msg.Type == MessageType.DeviceAcknowledgement)
                        //{
                        //	if (taskCompletions.ContainsKey(msg.Source))
                        //	{
                        //		var tcs = taskCompletions[msg.Source];
                        //		if (!tcs.Task.IsCompleted)
                        //			tcs.SetResult(msg);
                        //	}
                        //}
                        else
                        {
                            switch (msg.Type)
                            {
                                case MessageType.LightState:
                                    if (DiscoveredBulbs.ContainsKey(e.RemoteAddress.ToString()))
                                    {
                                        var bulb = DiscoveredBulbs[e.RemoteAddress.ToString()];

                                        bulb.State = (LightStateResponse) msg;
                                    }

                                    break;
                            }
                        }
                    }
                Debug.WriteLine("Received from {0}:{1}", remote.RawName,
                    string.Join(",", (from a in data select a.ToString("X2")).ToArray()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message}");
            }
        }

        private Task<T> BroadcastMessageAsync<T>(HostName hostName, FrameHeader header, MessageType type,
            params object[] args)
            where T : LifxResponse

        {
            var payload = new List<byte>();
            if (args != null)
                foreach (var arg in args)
                    if (arg is ushort)
                        payload.AddRange(BitConverter.GetBytes((ushort) arg));
                    else if (arg is uint)
                        payload.AddRange(BitConverter.GetBytes((uint) arg));
                    else if (arg is byte)
                        payload.Add((byte) arg);
                    else if (arg is byte[])
                        payload.AddRange((byte[]) arg);
                    else if (arg is string)
                        payload.AddRange(
                            Encoding.UTF8.GetBytes(((string) arg).PadRight(32)
                                .Take(32)
                                .ToArray())); //All strings are 32 bytes
                    else
                        throw new NotSupportedException(args.GetType().FullName);
            return BroadcastMessagePayloadAsync<T>(hostName, header, type, payload.ToArray());
        }

        private async Task<T> BroadcastMessagePayloadAsync<T>(HostName hostName, FrameHeader header, MessageType type,
            byte[] payload)
            where T : LifxResponse
        {
#if DEBUG
            var ms = new MemoryStream();
            await WritePacketToStreamAsync(ms.AsOutputStream(), header, (ushort) type, payload).ConfigureAwait(false);
            var data = ms.ToArray();
            Debug.WriteLine(
                string.Join(",", (from a in data select a.ToString("X2")).ToArray()));
#endif
            if (hostName == null)
                hostName = new HostName("255.255.255.255");
            TaskCompletionSource<T> tcs = null;
            if ( //header.AcknowledgeRequired && 
                header.Identifier > 0 &&
                typeof(T) != typeof(UnknownResponse))
            {
                tcs = new TaskCompletionSource<T>();
                Action<LifxResponse> action = r =>
                {
                    if (!tcs.Task.IsCompleted)
                        if (r.GetType() == typeof(T))
                            tcs.SetResult((T) r);
                };
                taskCompletions[header.Identifier] = action;
            }

            using (var stream = await _socket.GetOutputStreamAsync(hostName, Port))
            {
                await WritePacketToStreamAsync(stream, header, (ushort) type, payload).ConfigureAwait(false);
            }
            var result = default(T);
            if (tcs != null)
            {
                var _ = Task.Delay(1000)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCompleted)
                            tcs.TrySetException(new TimeoutException());
                    });
                try
                {
                    result = await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    taskCompletions.Remove(header.Identifier);
                }
            }
            return result;
        }

        private LifxResponse ParseMessage(byte[] packet)
        {
            using (var ms = new MemoryStream(packet))
            {
                var header = new FrameHeader();
                var br = new BinaryReader(ms);
                //frame
                var size = br.ReadUInt16();
                if (packet.Length != size || size < 36)
                {
                    return null;
                    throw new Exception("Invalid packet");
                }
                var a = br.ReadUInt16(); //origin:2, reserved:1, addressable:1, protocol:12
                var source = br.ReadUInt32();
                //frame address
                var target = br.ReadBytes(8);
                ms.Seek(6, SeekOrigin.Current); //skip reserved
                var b = br.ReadByte(); //reserved:6, ack_required:1, res_required:1, 
                header.Sequence = br.ReadByte();
                //protocol header
                var nanoseconds = br.ReadUInt64();
                header.AtTime = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
                var type = (MessageType) br.ReadUInt16();
                ms.Seek(2, SeekOrigin.Current); //skip reserved
                byte[] payload = null;
                if (size > 36)
                    payload = br.ReadBytes(size - 36);
                return LifxResponse.Create(header, type, source, payload);
            }
        }

        private async Task WritePacketToStreamAsync(IOutputStream outStream, FrameHeader header, ushort type,
            byte[] payload)
        {
            using (var dw = new DataWriter(outStream) {ByteOrder = ByteOrder.LittleEndian})
            {
                //BinaryWriter bw = new BinaryWriter(ms);

                #region Frame

                //size uint16
                dw.WriteUInt16((ushort) ((payload != null ? payload.Length : 0) + 36)); //length
                // origin (2 bits, must be 0), reserved (1 bit, must be 0), addressable (1 bit, must be 1), protocol 12 bits must be 0x400) = 0x1400
                dw.WriteUInt16(0x3400); //protocol
                dw.WriteUInt32(header
                    .Identifier); //source identifier - unique value set by the client, used by responses. If 0, responses are broadcasted instead

                #endregion Frame

                #region Frame address

                //The target device address is 8 bytes long, when using the 6 byte MAC address then left - 
                //justify the value and zero-fill the last two bytes. A target device address of all zeroes effectively addresses all devices on the local network
                dw.WriteBytes(header.TargetMacAddress); // target mac address - 0 means all devices
                dw.WriteBytes(new byte[] {0, 0, 0, 0, 0, 0}); //reserved 1

                //The client can use acknowledgements to determine that the LIFX device has received a message. 
                //However, when using acknowledgements to ensure reliability in an over-burdened lossy network ... 
                //causing additional network packets may make the problem worse. 
                //Client that don't need to track the updated state of a LIFX device can choose not to request a 
                //response, which will reduce the network burden and may provide some performance advantage. In
                //some cases, a device may choose to send a state update response independent of whether res_required is set.
                if (header.AcknowledgeRequired && header.ResponseRequired)
                    dw.WriteByte(0x03);
                else if (header.AcknowledgeRequired)
                    dw.WriteByte(0x02);
                else if (header.ResponseRequired)
                    dw.WriteByte(0x01);
                else
                    dw.WriteByte(0x00);
                //The sequence number allows the client to provide a unique value, which will be included by the LIFX 
                //device in any message that is sent in response to a message sent by the client. This allows the client
                //to distinguish between different messages sent with the same source identifier in the Frame. See
                //ack_required and res_required fields in the Frame Address.
                dw.WriteByte(header.Sequence);

                #endregion Frame address

                #region Protocol Header

                //The at_time value should be zero for Set and Get messages sent by a client.
                //For State messages sent by a device, the at_time will either be the device
                //current time when the message was received or zero. StateColor is an example
                //of a message that will return a non-zero at_time value
                if (header.AtTime > DateTime.MinValue)
                {
                    var time = header.AtTime.ToUniversalTime();
                    dw.WriteUInt64((ulong) (time - new DateTime(1970, 01, 01)).TotalMilliseconds * 10); //timestamp
                }
                else
                {
                    dw.WriteUInt64(0);
                }

                #endregion Protocol Header

                dw.WriteUInt16(type); //packet _type
                dw.WriteUInt16(0); //reserved
                if (payload != null)
                    dw.WriteBytes(payload);
                await dw.StoreAsync();
            }
        }
    }

    internal class FrameHeader
    {
        public bool AcknowledgeRequired;
        public DateTime AtTime;
        public uint Identifier;
        public bool ResponseRequired;
        public byte Sequence;
        public byte[] TargetMacAddress;

        public FrameHeader()
        {
            Identifier = 0;
            Sequence = 0;
            AcknowledgeRequired = false;
            ResponseRequired = false;
            TargetMacAddress = new byte[] {0, 0, 0, 0, 0, 0, 0, 0};
            AtTime = DateTime.MinValue;
        }
    }
}