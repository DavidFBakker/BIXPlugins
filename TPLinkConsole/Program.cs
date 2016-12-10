using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TPLinkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine("Usage: address On/Off <port=9999>");
                return;
            }

            switch (args[1].ToLower())
            {
                case "on":
                    TPLink.Control.On(args[0]);
                    break;
                case "off":
                    TPLink.Control.Off(args[0]);
                    break;
            }
           
        }
    }
}
