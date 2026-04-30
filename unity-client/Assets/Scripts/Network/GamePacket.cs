namespace Astrion.Network
{
    public class GamePacket
    {
        public PacketType Type { get; set; }
        public string Payload { get; set; }

        public GamePacket(PacketType type, string payload)
        {
            Type = type;
            Payload = payload;
        }
    }
}
