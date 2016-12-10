using System;
using System.Net;
using System.Net.Sockets;

namespace TPLink
{
    public static class Control
    {
        private static readonly string payload_on = @"AAAAKtDygfiL/5r31e+UtsWg1Iv5nPCR6LfEsNGlwOLYo4HyhueT9tTu36Lfog==";
        private static readonly string payload_off = @"AAAAKtDygfiL/5r31e+UtsWg1Iv5nPCR6LfEsNGlwOLYo4HyhueT9tTu3qPeow==";

        public static void On(string address, int port = 9999)
        {
            //byte[] msg = Encoding.ASCII.GetBytes(Base64Decode(payload_on));
            var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tpEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            try
            {
                sender.Connect(tpEndPoint);
                sender.Send(Base64Decode(payload_on));
            }
            catch (Exception ex)
            {
            }
        }

        public static void Off(string address, int port = 9999)
        {
            //byte[] msg = Encoding.ASCII.GetBytes(Base64Decode(payload_off));
            var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tpEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            try
            {
                sender.Connect(tpEndPoint);
                sender.Send(Base64Decode(payload_off));
            }
            catch (Exception ex)
            {
            }
        }

        public static byte[] Base64Decode(string base64EncodedData)
        {
            return Convert.FromBase64String(base64EncodedData);
        }
    }
}