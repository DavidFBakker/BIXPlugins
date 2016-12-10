using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BixPlugins
{
    public class BIXColor2s : List<BIXColor>
    {
    }

    public static class BIXColors
    {
        private static Dictionary<string, BIXColor> _colors;

        public static Dictionary<string, BIXColor> Colors
        {
            get
            {
                if (_colors == null)
                {
                    _colors = new Dictionary<string, BIXColor>();
                    var colorsArray = Enum.GetValues(typeof(KnownColor));
                    var allColors = new KnownColor[colorsArray.Length];

                    Array.Copy(colorsArray, allColors, colorsArray.Length);

                    foreach (var color in colorsArray)
                    {
                        var colorValue = Color.FromName(color.ToString());
                        var bixColor = new BIXColor
                        {
                            Name = color.ToString().ToLower(),
                            Hue = colorValue.GetHue(),
                            Saturation = colorValue.GetSaturation(),
                            Brightness = colorValue.GetBrightness(),
                            Hex = $"#{colorValue.R:X2}{colorValue.G:X2}{colorValue.B:X2}"
                        };
                        _colors[bixColor.Name] = bixColor;
                    }
                }
                return _colors;
            }
            set { _colors = value; }
        }
    }

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