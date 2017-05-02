using System;
using BixPlugins.BixLIFX;

namespace BixLIFXConsole
{
    internal class Program
    {
        // private const int commandSends = 2;
        //   private static readonly ObservableCollection<LightBulb> bulbs = new ObservableCollection<LightBulb>();
        //   private static LifxClient client;

        private static void Main(string[] args)
        {
            BixLIFX.Init();

         //   Console.WriteLine(BixLIFX.ColorsString);
            Console.ReadLine();
        }
    }
}