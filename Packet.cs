using System;

namespace PoPM
{
    /// <summary>
    /// A general packet to be sent over the wire.
    /// </summary>
    public class Packet
    {
        public PacketType Id;

        public Guid Sender;

        public byte[] Data;
    }
}