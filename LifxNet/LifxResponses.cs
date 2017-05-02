using System;
using System.Text;

namespace LifxNet
{
    /// <summary>
    ///     Base class for LIFX response types
    /// </summary>
    public abstract class LifxResponse
    {
        internal LifxResponse()
        {
        }

        internal FrameHeader Header { get; private set; }
        internal byte[] Payload { get; private set; }
        internal MessageType Type { get; private set; }
        internal uint Source { get; private set; }

        internal static LifxResponse Create(FrameHeader header, MessageType type, uint source, byte[] payload)
        {
            LifxResponse response = null;
            switch (type)
            {
                case MessageType.DeviceAcknowledgement:
                    response = new AcknowledgementResponse(payload);
                    break;
                case MessageType.DeviceStateLabel:
                    response = new StateLabelResponse(payload);
                    break;
                case MessageType.LightState:
                    response = new LightStateResponse(payload);
                    break;
                case MessageType.LightStatePower:
                    response = new LightPowerResponse(payload);
                    break;
                case MessageType.DeviceStateVersion:
                    response = new StateVersionResponse(payload);
                    break;
                case MessageType.DeviceStateHostFirmware:
                    response = new StateHostFirmwareResponse(payload);
                    break;
                case MessageType.DeviceStateService:
                    response = new StateServiceResponse(payload);
                    break;
                default:
                    response = new UnknownResponse(payload);
                    break;
            }
            response.Header = header;
            response.Type = type;
            response.Payload = payload;
            response.Source = source;
            return response;
        }
    }

    /// <summary>
    ///     Response to any message sent with ack_required set to 1.
    /// </summary>
    internal class AcknowledgementResponse : LifxResponse
    {
        internal AcknowledgementResponse(byte[] payload)
        {
        }
    }

    /// <summary>
    ///     Response to GetService message.
    ///     Provides the device Service and port.
    ///     If the Service is temporarily unavailable, then the port value will be 0.
    /// </summary>
    internal class StateServiceResponse : LifxResponse
    {
        internal StateServiceResponse(byte[] payload)
        {
            Service = payload[0];
            Port = BitConverter.ToUInt32(payload, 1);
        }

        public byte Service { get; set; }
        public uint Port { get; }
    }

    /// <summary>
    ///     Response to GetLabel message. Provides device label.
    /// </summary>
    internal class StateLabelResponse : LifxResponse
    {
        internal StateLabelResponse(byte[] payload)
        {
            if (payload != null)
                Label = Encoding.UTF8.GetString(payload, 0, payload.Length).Replace("\0", "");
        }

        public string Label { get; }
    }

    /// <summary>
    ///     Sent by a device to provide the current light state
    /// </summary>
    public class LightStateResponse : LifxResponse
    {
        internal LightStateResponse(byte[] payload)
        {
            Hue = BitConverter.ToUInt16(payload, 0);
            Saturation = BitConverter.ToUInt16(payload, 2);
            Brightness = BitConverter.ToUInt16(payload, 4);
            Kelvin = BitConverter.ToUInt16(payload, 6);
            IsOn = BitConverter.ToUInt16(payload, 10) > 0;
            Label = Encoding.UTF8.GetString(payload, 12, 32).Replace("\0", "");
        }

        /// <summary>
        ///     Hue
        /// </summary>
        public ushort Hue { get; set; }

        /// <summary>
        ///     Saturation (0=desaturated, 65535 = fully saturated)
        /// </summary>
        public ushort Saturation { get; set; }

        /// <summary>
        ///     Brightness (0=off, 65535=full brightness)
        /// </summary>
        public ushort Brightness { get; set; }

        /// <summary>
        ///     Bulb color temperature
        /// </summary>
        public ushort Kelvin { get; set; }

        /// <summary>
        ///     Power state
        /// </summary>
        public bool IsOn { get; set; }

        /// <summary>
        ///     Light label
        /// </summary>
        public string Label { get; }
    }

    public class LightPowerResponse : LifxResponse
    {
        internal LightPowerResponse(byte[] payload)
        {
            IsOn = BitConverter.ToUInt16(payload, 0) > 0;
        }

        public bool IsOn { get; }
    }

    /// <summary>
    ///     Response to GetVersion message.	Provides the hardware version of the device.
    /// </summary>
    public class StateVersionResponse : LifxResponse
    {
        internal StateVersionResponse(byte[] payload)
        {
            Vendor = BitConverter.ToUInt32(payload, 0);
            Product = BitConverter.ToUInt32(payload, 4);
            Version = BitConverter.ToUInt32(payload, 8);
        }

        /// <summary>
        ///     Vendor ID
        /// </summary>
        public uint Vendor { get; }

        /// <summary>
        ///     Product ID
        /// </summary>
        public uint Product { get; }

        /// <summary>
        ///     Hardware version
        /// </summary>
        public uint Version { get; }
    }

    /// <summary>
    ///     Response to GetHostFirmware message. Provides host firmware information.
    /// </summary>
    public class StateHostFirmwareResponse : LifxResponse
    {
        internal StateHostFirmwareResponse(byte[] payload)
        {
            var nanoseconds = BitConverter.ToUInt64(payload, 0);
            Build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
            //8..15 UInt64 is reserved
            Version = BitConverter.ToUInt32(payload, 16);
        }

        /// <summary>
        ///     Firmware build time
        /// </summary>
        public DateTime Build { get; }

        /// <summary>
        ///     Firmware version
        /// </summary>
        public uint Version { get; }
    }

    internal class UnknownResponse : LifxResponse
    {
        internal UnknownResponse(byte[] payload)
        {
        }
    }
}