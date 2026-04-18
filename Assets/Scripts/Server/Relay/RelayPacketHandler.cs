using Protocol;
using UnityEngine;

public class RelayPacketHandler: Singleton<RelayPacketHandler>
{
    public void HandleRelayPacket(S_RELAY_PACKET relayPacket)
    {
        var senderId = (int)relayPacket.SenderId;
        var packetId = (PacketId)relayPacket.PacketId;
        var payload = relayPacket.Payload.ToByteArray();
        
        if (IsClientPacket(packetId))
        {
            // Peer가 보낸 패킷
            PeerPacketHandler.Instance.HandlePeerPacket(senderId, packetId, payload);
        }
        else if (IsServerPacket(packetId))
        {
            // Host가 보낸 패킷
            HostPacketHandler.Instance.HandlePacket(packetId, payload);
        }
        else
        {
            Debug.LogWarning($"[RelayPacketHandler] 알 수 없는 packetId: {packetId}");
        }
    }

    private bool IsClientPacket(PacketId id)
    {
        return id.ToString().StartsWith("PKT_C_");
    }
    private bool IsServerPacket(PacketId id)
    {
        return id.ToString().StartsWith("PKT_S_");
    }
    public void OnDisconnected()
    {
        Debug.Log("서버와의 연결이 해제되었습니다.");
        // TODO: UI 갱신, 재접속 안내, 게임 상태 초기화 등 필요한 작업 추가
    }

    public void HandleRelayPacket(PacketId packetId, byte[] data)
    {
        Debug.Log($"Received packet with ID: {packetId}, Size: {data.Length} bytes");
        switch (packetId)
        {
            case PacketId.PKT_S_RELAY_PACKET:
                RelayPacketHandler.Instance.HandleRelayPacket(S_RELAY_PACKET.Parser.ParseFrom(data));
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }
}
