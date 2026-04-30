namespace Astrion.Network
{
    public enum PacketType : byte
    {
        // Client -> Server
        Login = 0x01,
        Move = 0x02,
        Chat = 0x03,
        Attack = 0x04,
        CharacterList = 0x05,
        CharacterCreate = 0x06,
        CharacterDelete = 0x07,

        // Server -> Client
        LoginResult = 0x81,
        SpawnPlayer = 0x82,
        DespawnPlayer = 0x83,
        PlayerMoved = 0x84,
        ChatMessage = 0x85,
        WorldState = 0x86,
        CharacterListResult = 0x87,
        CharacterCreateResult = 0x88,
        CharacterDeleteResult = 0x89
    }
}
