using System;
using System.Collections.ObjectModel;
using BixPlugins.BixLIFX;
using LifxNet;

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
            Console.WriteLine(BixLIFX.ColorsString);
            Console.ReadLine();
        }
    }
}