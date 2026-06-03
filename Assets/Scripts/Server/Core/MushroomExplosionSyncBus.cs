using System;
using Google.Protobuf;
using Protocol;

public static class MushroomExplosionSyncBus
{
    public static event Action<byte[]> OnExplodePayload;

    public static void Publish(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
            return;

        OnExplodePayload?.Invoke(payload);
    }

    public static void SendExplodeRequest(byte[] payload)
    {
        SendRelay(PacketId.PKT_C_MUSHROOM_EXPLODE, payload, requireHostAuthority: true);
    }

    public static void BroadcastExplodeFromHost(byte[] payload)
    {
        SendRelay(PacketId.PKT_S_MUSHROOM_EXPLODE, payload, requireHostAuthority: false);
    }

    private static void SendRelay(PacketId packetId, byte[] payload, bool requireHostAuthority)
    {
        if (NetManager.Instance == null)
            return;

        C_RELAY_PACKET relayPacket = new C_RELAY_PACKET
        {
            RequireHostAuthority = requireHostAuthority,
            PacketId = (uint)packetId,
            Payload = ByteString.CopyFrom(payload ?? Array.Empty<byte>())
        };

        NetManager.Instance.SendPacket(PacketId.PKT_C_RELAY_PACKET, relayPacket);
    }
}
