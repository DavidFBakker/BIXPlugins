namespace BixPlugins
{
    public class BIXColor
    {
        public string Name { get; set; }
        public float Hue { get; set; }
        public float Saturation { get; set; }
        public float Brightness { get; set; }
        public string Hex { get; set; }

        public string TableRow
        {
            get
            {                              
                //StringBuilder ret = new StringBuilder();
                //ret.Append("<tr>");
                //ret.Append($"<td>{Name}</td>");
                //ret.Append($"<td bgcolor=\"{Hex}\"></td>");
                //ret.Append("</tr>");
                var str = $"<tr><td>{Name}</td><td bgcolor=\"{Hex}\" width=\"50px\"></td></tr>";
                return str;
            }
        }

        public ushort LIFXHue
        {
            get
            {
                var diff = 65535/360.0;
                var ret2 = Hue*diff;
                var ret = (ushort) ret2;
                return ret;
            }
        }

        public ushort LIFXSaturation
        {
            get
            {
                var diff = 65535;
                var ret2 = Saturation*diff;
                var ret = (ushort) ret2;
                return ret;
            }
        }

        public ushort LIFXBrightness
        {
            get
            {
                var diff = 65535;
                var ret2 = Brightness*diff;
                var ret = (ushort) ret2;
                return ret;
            }
        }

        public ushort Kelvin { get; set; }
    }
}