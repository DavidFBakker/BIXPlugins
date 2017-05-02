using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;

namespace LifxNet
{
    public partial class LifxClient : IDisposable
    {
        private readonly Dictionary<uint, Action<LifxResponse>> taskCompletions =
            new Dictionary<uint, Action<LifxResponse>>();

        /// <summary>
        ///     Turns a bulb on using the provided transition time
        /// </summary>
        /// <param name="bulb"></param>
        /// <param name="transitionDuration"></param>
        /// <returns></returns>
        public Task TurnBulbOnAsync(LightBulb bulb, TimeSpan transitionDuration)
        {
            Debug.WriteLine("Sending TurnBulbOn to {0}", bulb.HostName);
            return SetLightPowerAsync(bulb, transitionDuration, true);
        }

        /// <summary>
        ///     Turns a bulb off using the provided transition time
        /// </summary>
        public Task TurnBulbOffAsync(LightBulb bulb, TimeSpan transitionDuration)
        {
            Debug.WriteLine("Sending TurnBulbOff to {0}", bulb.HostName);
            return SetLightPowerAsync(bulb, transitionDuration, false);
        }

        private async Task SetLightPowerAsync(LightBulb bulb, TimeSpan transitionDuration, bool isOn)
        {
            if (bulb == null)
                throw new ArgumentNullException("bulb");
            if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
                transitionDuration.Ticks < 0)
                throw new ArgumentOutOfRangeException("transitionDuration");

            var header = new FrameHeader
            {
                Identifier = (uint) randomizer.Next(),
                AcknowledgeRequired = true
            };

            var b = BitConverter.GetBytes((ushort) transitionDuration.TotalMilliseconds);
            bulb.IsOn = isOn;
            bulb.State.IsOn = isOn;
            await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header, MessageType.LightSetPower,
                    (ushort) (isOn ? 65535 : 0), b
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets the current power state for a light bulb
        /// </summary>
        /// <param name="bulb"></param>
        /// <returns></returns>
        public async Task<bool> GetLightPowerAsync(LightBulb bulb)
        {
            var header = new FrameHeader
            {
                Identifier = (uint) randomizer.Next(),
                AcknowledgeRequired = true
            };
            return (await BroadcastMessageAsync<LightPowerResponse>(
                    bulb.HostName, header, MessageType.LightGetPower)
                .ConfigureAwait(false)).IsOn;
        }

        /// <summary>
        ///     Sets color and temperature for a bulb
        /// </summary>
        /// <param name="bulb"></param>
        /// <param name="color"></param>
        /// <param name="kelvin"></param>
        /// <returns></returns>
        public Task SetColorAsync(LightBulb bulb, Color color, ushort kelvin)
        {
            return SetColorAsync(bulb, color, kelvin, TimeSpan.Zero);
        }

        /// <summary>
        ///     Sets color and temperature for a bulb and uses a transition time to the provided state
        /// </summary>
        /// <param name="bulb"></param>
        /// <param name="color"></param>
        /// <param name="kelvin"></param>
        /// <param name="transitionDuration"></param>
        /// <returns></returns>
        public Task SetColorAsync(LightBulb bulb, Color color, ushort kelvin, TimeSpan transitionDuration)
        {
            var hsl = Utilities.RgbToHsl(color);
            return SetColorAsync(bulb, hsl[0], hsl[1], hsl[2], kelvin, transitionDuration);
        }

        /// <summary>
        ///     Sets brightness for a bulb and uses a transition time to the provided state
        /// </summary>
        /// <param name="bulb"></param>
        /// <param name="color"></param>
        /// <param name="kelvin"></param>
        /// <param name="transitionDuration"></param>
        /// <returns></returns>
        public Task SetBrightnesAsync(LightBulb bulb, double brightnessPercent, TimeSpan transitionDuration)
        {
            var dim = (ushort) (65535 * (brightnessPercent / 100.0));
            var hue = bulb.State.Hue;
            var saturation = bulb.State.Saturation;

            return SetColorAsync(bulb, hue, saturation, dim, 0, transitionDuration);
        }

        /// <summary>
        ///     Sets color and temperature for a bulb and uses a transition time to the provided state
        /// </summary>
        /// <param name="bulb">Light bulb</param>
        /// <param name="hue">0..65535</param>
        /// <param name="saturation">0..65535</param>
        /// <param name="brightness">0..65535</param>
        /// <param name="kelvin">2500..9000</param>
        /// <param name="transitionDuration"></param>
        /// <returns></returns>
        public async Task SetColorAsync(LightBulb bulb,
            ushort hue,
            ushort saturation,
            ushort brightness,
            ushort kelvin,
            TimeSpan transitionDuration)
        {
            if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
                transitionDuration.Ticks < 0)
                throw new ArgumentOutOfRangeException("transitionDuration");
            if (kelvin != 0 && (kelvin < 2500 || kelvin > 9000))
                throw new ArgumentOutOfRangeException("kelvin", "Kelvin must be between 2500 and 9000");

            Debug.WriteLine("Setting color to {0}", bulb.HostName);
            var header = new FrameHeader
            {
                Identifier = (uint) randomizer.Next(),
                AcknowledgeRequired = true
            };
            var duration = (uint) transitionDuration.TotalMilliseconds;
            var durationBytes = BitConverter.GetBytes(duration);
            //var h = BitConverter.GetBytes(hue);
            //var s = BitConverter.GetBytes(saturation);
            //var b = BitConverter.GetBytes(brightness);
            //var k = BitConverter.GetBytes(kelvin);

            bulb.State.Hue = hue;
            bulb.State.Saturation = saturation;
            bulb.State.Brightness = brightness;
            bulb.State.Kelvin = kelvin;

            await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
                MessageType.LightSetColor, (byte) 0x00, //reserved
                hue, saturation, brightness, kelvin, //HSBK
                duration
            );
        }


        //public async Task SetBrightnessAsync(LightBulb bulb,
        //    UInt16 brightness,
        //    TimeSpan transitionDuration)
        //{
        //    if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
        //        transitionDuration.Ticks < 0)
        //        throw new ArgumentOutOfRangeException("transitionDuration");

        //    FrameHeader header = new FrameHeader()
        //    {
        //        Identifier = (uint)randomizer.Next(),
        //        AcknowledgeRequired = true
        //    };
        //    UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;
        //    var durationBytes = BitConverter.GetBytes(duration);
        //    var b = BitConverter.GetBytes(brightness);

        //    await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
        //        MessageType.SetLightBrightness, brightness, duration
        //    );
        //}

        /// <summary>
        ///     Gets the current state of the bulb
        /// </summary>
        /// <param name="bulb"></param>
        /// <returns></returns>
        public Task<LightStateResponse> GetLightStateAsync(LightBulb bulb)
        {
            var header = new FrameHeader
            {
                Identifier = (uint) randomizer.Next(),
                AcknowledgeRequired = false
            };
            return BroadcastMessageAsync<LightStateResponse>(
                bulb.HostName, header, MessageType.LightGet);
        }
    }
}