using System;
using System.IO;

namespace LifxNet
{
    internal abstract class LifxPacket
    {
        protected LifxPacket(ushort type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }

        protected LifxPacket(ushort type, object[] data)
        {
            Type = type;
            using (var ms = new MemoryStream())
            {
                var bw = new StreamWriter(ms);
                foreach (var obj in data)
                    if (obj is byte)
                        bw.Write((byte) obj);
                    else if (obj is byte[])
                        bw.Write((byte[]) obj);
                    else if (obj is ushort)
                        bw.Write((ushort) obj);
                    else if (obj is uint)
                        bw.Write((uint) obj);
                    else
                        throw new NotImplementedException();
                Payload = ms.ToArray();
            }
        }

        internal byte[] Payload { get; }

        internal ushort Type { get; }

        public static LifxPacket FromByteArray(byte[] data)
        {
            //			preambleFields = [
            //				{ name: "size"       , type:type.uint16_le },
            //				{ name: "protocol"   , type:type.uint16_le },
            //				{ name: "reserved1"  , type:type.byte4 }    ,
            //				{ name: "bulbAddress", type:type.byte6 }    ,
            //				{ name: "reserved2"  , type:type.byte2 }    ,
            //				{ name: "site"       , type:type.byte6 }    ,
            //				{ name: "reserved3"  , type:type.byte2 }    ,
            //				{ name: "timestamp"  , type:type.uint64 }   ,
            //				{ name: "packetType" , type:type.uint16_le },
            //				{ name: "reserved4"  , type:type.byte2 }    ,
            //			];
            var ms = new MemoryStream(data);
            var br = new BinaryReader(ms);
            //Header
            var len = br.ReadUInt16(); //ReverseBytes(br.ReadUInt16()); //size uint16
            var protocol = br.ReadUInt16(); // ReverseBytes(br.ReadUInt16()); //origin = 0
            var identifier = br.ReadUInt32();
            var bulbAddress = br.ReadBytes(6);
            var reserved2 = br.ReadBytes(2);
            var site = br.ReadBytes(6);
            var reserved3 = br.ReadBytes(2);
            var timestamp = br.ReadUInt64();
            var packetType = br.ReadUInt16(); // ReverseBytes(br.ReadUInt16());
            var reserved4 = br.ReadBytes(2);
            byte[] payload = { };
            if (len > 0)
                payload = br.ReadBytes(len);
            LifxPacket packet = new UnknownPacket(packetType, payload)
            {
                BulbAddress = bulbAddress,
                TimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp),
                Site = site
            };
            //packet.Identifier = identifier;
            return packet;
        }

        private class UnknownPacket : LifxPacket
        {
            public UnknownPacket(ushort packetType, byte[] payload) : base(packetType, payload)
            {
            }

            public byte[] BulbAddress { get; set; }
            public DateTime TimeStamp { get; set; }
            public byte[] Site { get; set; }
        }
    }
}