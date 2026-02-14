using UnityEngine;
using Google.Protobuf;
using Protocol;

public class PacketManager : Singleton<PacketManager>
{
    public void HandlePacket(PacketId packetId, byte[] data)
    {
        Debug.Log($"Received packet with ID: {packetId}, Size: {data.Length} bytes");
        switch (packetId)
        {
            case PacketId.PKT_S_LOGIN:  // ✅ 사용!
                HandleLoginResult(data);
                break;

            case PacketId.PKT_S_ENTER_GAME:  // ✅ 사용!
                HandleEnterGameResult(data);
                break;

            case PacketId.PKT_S_CHAT:  // ✅ 사용!
                HandleChat(data);
                break;
        }
    }

    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data);  // ← S_LOGIN 사용

        if (result.Success)
        {
            Debug.Log($"✓ Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}");
            Debug.Log($"  Player Name: {result.Player.Name}");
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data);  // ← S_ENTER_GAME 사용

        if (result.Success)
        {
            Debug.Log("✓ Entered Game Successfully!");
        }
        else
        {
            Debug.LogError("✗ Failed to Enter Game!");
        }
    }

    private void HandleChat(byte[] data)
    {
        S_CHAT chat = S_CHAT.Parser.ParseFrom(data);  // ← S_CHAT 사용
        Debug.Log($"💬 [{chat.PlayerId}]: {chat.Msg}");
    }
}
