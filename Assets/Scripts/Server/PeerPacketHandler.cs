using UnityEngine;
using Google.Protobuf;
using Protocol;

/// <summary>
/// 호스트가 피어로부터 받은 패킷을 처리하는 클래스
/// NetManager의 피어 receive 루프에서 호출됨
/// 각 C_ 패킷에 대한 이벤트를 정의하여 게임 로직에서 구독할 수 있도록 함
/// 예: 플레이어 이동, 채팅 메시지, 애니메이션 상태 변경, 아이템 상호작용 등
/// </summary>
public class PeerPacketHandler : Singleton<PeerPacketHandler>
{
    // 이벤트 (호스트가 피어로부터 받은 C_ 패킷들)
    public event System.Action<int, C_MOVE> OnPeerMoveEvent;
    public event System.Action<int, C_CHAT> OnPeerChatEvent;
    public event System.Action<int, C_PLAYER_ANIMATION> OnPeerAnimationEvent;
    public event System.Action<int, C_OBJECT_PICKUP> OnPeerItemAttachedEvent;
    public event System.Action<int, C_OBJECT_DROP> OnPeerItemDetachedEvent;

    /// <summary>
    /// 호스트가 피어로부터 받은 패킷 처리
    /// NetManager의 피어 receive 루프에서 호출됨
    /// </summary>
    public void HandlePeerPacket(int peerId, PacketId packetId, byte[] data)
    {
        Debug.Log($"[Peer {peerId}] Received packet: {packetId}, Size: {data.Length} bytes");

        switch (packetId)
        {
            case PacketId.PKT_C_MOVE:
                HandlePeerMove(peerId, data);
                break;
            case PacketId.PKT_C_CHAT:
                HandlePeerChat(peerId, data);
                break;
            case PacketId.PKT_C_PLAYER_ANIMATION:
                HandlePeerAnimation(peerId, data);
                break;
            case PacketId.PKT_C_OBJECT_PICKUP:
                HandlePeerItemAttached(peerId, data);
                break;
            case PacketId.PKT_C_OBJECT_DROP:
                HandlePeerItemDetached(peerId, data);
                break;
            default:
                Debug.LogWarning($"[Peer {peerId}] Unhandled packet ID: {packetId}");
                break;
        }
    }

    private void HandlePeerMove(int peerId, byte[] data)
    {
        C_MOVE packet = C_MOVE.Parser.ParseFrom(data);
        Debug.Log($"[Peer {peerId}] Move: Pos({packet.Pos.X}, {packet.Pos.Y}, {packet.Pos.Z})");

        OnPeerMoveEvent?.Invoke(peerId, packet);

        // TODO: 다른 피어들에게 브로드캐스트
        // BroadcastToOtherPeers(peerId, packet);
    }

    private void HandlePeerChat(int peerId, byte[] data)
    {
        C_CHAT packet = C_CHAT.Parser.ParseFrom(data);
        Debug.Log($"[Peer {peerId}] Chat: {packet.Msg}");

        OnPeerChatEvent?.Invoke(peerId, packet);

        // TODO: 다른 피어들에게 브로드캐스트
    }

    private void HandlePeerAnimation(int peerId, byte[] data)
    {
        C_PLAYER_ANIMATION packet = C_PLAYER_ANIMATION.Parser.ParseFrom(data);
        Debug.Log($"[Peer {peerId}] Animation: {packet.State}");

        OnPeerAnimationEvent?.Invoke(peerId, packet);
    }

    private void HandlePeerItemAttached(int peerId, byte[] data)
    {
        C_OBJECT_PICKUP packet = C_OBJECT_PICKUP.Parser.ParseFrom(data);
        Debug.Log($"[Peer {peerId}] Item Attached: {packet.ObjectId.ItemId}");

        OnPeerItemAttachedEvent?.Invoke(peerId, packet);
    }

    private void HandlePeerItemDetached(int peerId, byte[] data)
    {
        C_OBJECT_DROP packet = C_OBJECT_DROP.Parser.ParseFrom(data);
        Debug.Log($"[Peer {peerId}] Item Detached: {packet.ObjectId.ItemId}");

        OnPeerItemDetachedEvent?.Invoke(peerId, packet);
    }

    // ============== 브로드캐스트 로직 (나중에) ==============
    // public void BroadcastToOtherPeers(int senderPeerId, IMessage packet)
    // {
    //     // 모든 피어에게 전송 (보낸 피어 제외)
    //     // NetManager.Instance에서 _peerSockets 접근 필요
    // }
}
