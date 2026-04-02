using UnityEngine;
using Google.Protobuf;
using Protocol;
using System;

/// <summary>
/// 호스트가 피어로부터 받은 패킷을 처리하는 클래스
/// NetManager의 피어 receive 루프에서 호출됨
/// 각 C_ 패킷에 대한 이벤트를 정의하여 게임 로직에서 구독할 수 있도록 함
/// 예: 플레이어 이동, 채팅 메시지, 애니메이션 상태 변경, 아이템 상호작용 등
/// </summary>
public class PeerPacketHandler : Singleton<PeerPacketHandler>
{
    // 이벤트 (호스트가 피어로부터 받은 C_ 패킷들)
    public event Action<int, C_MOVE> OnPeerMoveEvent;
    public event Action<int, C_CHAT> OnPeerChatEvent;
    public event Action<int, C_PLAYER_ANIMATION> OnPeerAnimationEvent;
    public event Action<int, C_OBJECT_PICKUP> OnPeerItemAttachedEvent;
    public event Action<int, C_OBJECT_DROP> OnPeerItemDetachedEvent;
    public event Action<int, C_TEST_ENTER_GAME> OnPeerEnterGameEvent;
    /// <summary>
    /// 호스트가 피어로부터 받은 패킷 처리
    /// NetManager의 피어 receive 루프에서 호출됨
    /// </summary>
    public void HandlePeerPacket(int peerId, PacketId packetId, byte[] data)
    {
        //Debug.Log($"[Peer {peerId}] Received packet: {packetId}, Size: {data.Length} bytes");

        switch (packetId)
        {
            case PacketId.PKT_C_TEST_ENTER_GAME:
                Debug.Log($"[Peer {peerId}] Handling EnterGame packet");
                HandlePeerEnterGame(peerId, data);
                break;
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
            case PacketId.PKT_C_PLAYER_STAT_EVENT:
                HandlePeerStatEvent(peerId, data);
                break;
            default:
                Debug.LogWarning($"[Peer {peerId}] Unhandled packet ID: {packetId}");
                break;
        }
    }

    private void HandlePeerEnterGame(int peerId, byte[] data)
    {
        C_TEST_ENTER_GAME packet = C_TEST_ENTER_GAME.Parser.ParseFrom(data);

        // 1. S_PLAYER_ENTER 패킷 생성
        S_PLAYER_ENTER enterPacket = new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = peerId,
                Name = "Peer", // packet.Name이 null이면 기본값
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        };

        Debug.Log($"[Peer {peerId}] Created S_PLAYER_ENTER packet for new player");

        // 2. 모든 피어에게 브로드캐스트
        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_ENTER, enterPacket);
        // 3. 이벤트로 처리
        OnPeerEnterGameEvent?.Invoke(peerId, packet);
        // 4. 
        HostStatManager.Instance?.AddPlayer((ulong)peerId);
    }

    private void HandlePeerMove(int peerId, byte[] data)
    {
        C_MOVE packet = C_MOVE.Parser.ParseFrom(data);
        OnPeerMoveEvent?.Invoke(peerId, packet);

        S_MOVE relay = new S_MOVE
        {
            PlayerId = (ulong)peerId,
            Pos = packet.Pos?.Clone(),
            Rot = packet.Rot?.Clone()
        };

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_MOVE, relay);
    }

    private void HandlePeerChat(int peerId, byte[] data)
    {
        C_CHAT packet = C_CHAT.Parser.ParseFrom(data);
        OnPeerChatEvent?.Invoke(peerId, packet);

        S_CHAT relay = new S_CHAT
        {
            PlayerId = (ulong)peerId,
            Msg = packet.Msg
        };

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_CHAT, relay);
    }

    private void HandlePeerAnimation(int peerId, byte[] data)
    {
        C_PLAYER_ANIMATION packet = C_PLAYER_ANIMATION.Parser.ParseFrom(data);
        OnPeerAnimationEvent?.Invoke(peerId, packet);

        S_PLAYER_ANIMATION relay = new S_PLAYER_ANIMATION
        {
            PlayerId = (ulong)peerId,
            State = packet.State
        };

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_ANIMATION, relay);
    }

    private void HandlePeerItemAttached(int peerId, byte[] data)
    {
        C_OBJECT_PICKUP packet = C_OBJECT_PICKUP.Parser.ParseFrom(data);
        OnPeerItemAttachedEvent?.Invoke(peerId, packet);

        S_OBJECT_PICKUP relay = new S_OBJECT_PICKUP
        {
            Success = true,
            ObjectId = packet.ObjectId?.Clone(),
            PlayerId = (ulong)peerId,
            ErrorMsg = ""
        };

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_PICKUP, relay);
    }

    private void HandlePeerItemDetached(int peerId, byte[] data)
    {
        C_OBJECT_DROP packet = C_OBJECT_DROP.Parser.ParseFrom(data);
        OnPeerItemDetachedEvent?.Invoke(peerId, packet);

        S_OBJECT_DROP relay = new S_OBJECT_DROP
        {
            ObjectId = packet.ObjectId?.Clone(),
            PlayerId = (ulong)peerId
        };

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_DROP, relay);
    }

    private void HandlePeerStatEvent(int peerId, byte[] data)
    {
        var packet = C_PLAYER_STAT_EVENT.Parser.ParseFrom(data);

        var statManager = HostStatManager.Instance;

        if (packet.EventType == StatEventType.OxygenChanged && packet.Oxygen != null)
        {
            if (packet.Oxygen.ChangeType == OxygenChangeType.ConsumeNatural)
            {
                if (packet.TargetPlayerId == NetManager.Instance._playerId)
                {
                    return;
                }
                statManager.DecreaseOxygen(packet.TargetPlayerId);
            }
            else if (packet.Oxygen.ChangeType == OxygenChangeType.RestoreArea)
            {
                statManager.IncreaseOxygen(packet.TargetPlayerId);
            }
            else
            {
                Debug.LogWarning($"Unknown oxygen change type: {packet.Oxygen.ChangeType}");
            }
                
        }

        if (packet.EventType == StatEventType.DamageTaken && packet.Damage != null)
        {
            statManager.DecreaseHp(packet.TargetPlayerId, packet.Damage.DamageAmount);
        }
        else if (packet.EventType == StatEventType.Healed && packet.Heal != null)
        {
            statManager.IncreaseHp(packet.TargetPlayerId, packet.Heal.HealAmount);
        }

        // 상태 변경 후 브로드캐스트
        var stat = statManager.GetPlayerStat(packet.TargetPlayerId);
        if (stat != null)
        {
            var syncPacket = new S_PLAYER_STAT
            {
                PlayerId = packet.TargetPlayerId,
                Hp = stat.GetHp(),
                Oxygen = stat.GetOxygen()
            };

            Debug.Log($"전체 참여자에게 정보를 전달: {packet.TargetPlayerId}: HP={syncPacket.Hp}, Oxygen={syncPacket.Oxygen}");
            HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_STAT, syncPacket, true);
        }
    }
}
