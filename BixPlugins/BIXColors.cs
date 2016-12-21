using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BixPlugins
{
    //public class BIXColor2s : List<BIXColor>
    //{
    //}

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
}