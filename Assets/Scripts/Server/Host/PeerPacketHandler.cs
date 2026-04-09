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
    public event Action<int, C_PLAYER_STAT_EVENT> OnPeerStatEvent;
    public event Action<int, ulong> OnPeerSmeltRequestEvent;
    public event Action<int> OnPeerFurnaceRetrieveEvent;
    public event Action<int, C_OBJECT_SPAWN> OnPeerObjectSpawnEvent;
    public event Action<int, C_OBJECT_DESTROY> OnPeerObjectDestroyEvent;
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
            case PacketId.PKT_C_OBJECT_MOVE:
                HandlePeerObjectMove(peerId, data);
                break;
            case PacketId.PKT_C_PLAYER_STAT_EVENT:
                HandlePeerStatEvent(peerId, data);
                break;
            case PacketId.PKT_C_OBJECT_SMELT:
                HandlePeerSmeltRequest(peerId, data);
                break;
            case PacketId.PKT_C_FURNACE_RETRIEVE:
                HandlePeerFurnaceRetrieve(peerId, data);
                break;
            case PacketId.PKT_C_OBJECT_SPAWN:
                HandlePeerObjectSpawn(peerId, data);
                break;
            case PacketId.PKT_C_OBJECT_DESTROY:
                HandlePeerObjectDestroy(peerId, data);
                break;
            default:
                Debug.LogWarning($"[Peer {peerId}] Unhandled packet ID: {packetId}");
                break;
        }
    }

    private void HandlePeerEnterGame(int peerId, byte[] data)
    {
        C_TEST_ENTER_GAME packet = C_TEST_ENTER_GAME.Parser.ParseFrom(data);

        // 1. 피어 스탯 먼저 등록 (이후 패킷 처리 전에 반드시 등록되어야 함)
        HostStatManager.Instance?.AddPlayer((ulong)peerId);

        // 2. 피어의 S_PLAYER_ENTER 패킷 생성
        S_PLAYER_ENTER enterPacket = new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = peerId,
                Name = "Peer",
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        };

        // 3. 다른 피어들에게 브로드캐스트 (새로 들어온 피어 제외)
        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_ENTER, enterPacket, includeSender: false);

        // 4. 새로 들어온 피어에게 자신의 정보 전송 (playerId 세팅용, 스폰 제외 플래그 없음)
        HostNetManager.Instance.SendToPeer(peerId, PacketId.PKT_S_PLAYER_ENTER, enterPacket);

        // 5. 새로 들어온 피어에게 호스트 정보 전송
        S_PLAYER_ENTER hostEnterPacket = new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = (int)NetManager.Instance._playerId,
                Name = "Host",
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        };
        HostNetManager.Instance.SendToPeer(peerId, PacketId.PKT_S_PLAYER_ENTER, hostEnterPacket);

        // 6. PlayManager 이벤트 발생
        OnPeerEnterGameEvent?.Invoke(peerId, packet);
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

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_MOVE, relay, includeSender: false);
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

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_ANIMATION, relay, includeSender: false);
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

        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_PICKUP, relay, includeSender: false);
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


    private void HandlePeerObjectMove(int peerId, byte[] data)
    {
        C_OBJECT_MOVE packet = C_OBJECT_MOVE.Parser.ParseFrom(data);
        S_OBJECT_MOVE relay = new S_OBJECT_MOVE
        {
            ObjectId = packet.ObjectId?.Clone(),
            Pos = packet.Pos?.Clone(),
            Rot = packet.Rot?.Clone()
        };
        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_MOVE, relay, includeSender: false);
    }

    private void HandlePeerStatEvent(int peerId, byte[] data)
    {
        var packet = C_PLAYER_STAT_EVENT.Parser.ParseFrom(data);
        var statManager = HostStatManager.Instance;

        if (packet.EventType == StatEventType.OxygenChanged && packet.Oxygen != null)
        {
            if (packet.Oxygen.ChangeType == OxygenChangeType.ConsumeNatural)
            {
                if (packet.TargetPlayerId == NetManager.Instance._playerId) return;
                statManager.DecreaseOxygen(packet.TargetPlayerId);
            }
            else if (packet.Oxygen.ChangeType == OxygenChangeType.RestoreArea)
            {
                statManager.IncreaseOxygen(packet.TargetPlayerId);
            }
        }

        if (packet.EventType == StatEventType.DamageTaken && packet.Damage != null)
            statManager.DecreaseHp(packet.TargetPlayerId, packet.Damage.DamageAmount);
        else if (packet.EventType == StatEventType.Healed && packet.Heal != null)
            statManager.IncreaseHp(packet.TargetPlayerId, packet.Heal.HealAmount);

        // 에러 로그 없이 조회
        if (statManager.TryGetPlayerStat(packet.TargetPlayerId, out var stat))
        {
            var syncPacket = new S_PLAYER_STAT
            {
                PlayerId = packet.TargetPlayerId,
                Hp = stat.GetHp(),
                Oxygen = stat.GetOxygen()
            };

            HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_PLAYER_STAT, syncPacket, true);
            OnPeerStatEvent?.Invoke(peerId, packet); // 올바른 인수 타입으로 수정
        }
        else
        {
            Debug.LogWarning($"[HandlePeerStatEvent] Player {packet.TargetPlayerId} not registered yet, skipping.");
        }
    }

    public void HandlePeerSmeltRequest(int peerId, byte[] data)
    {
        var packet = C_OBJECT_SMELT.Parser.ParseFrom(data);
        Debug.Log($"Received smelt request from Peer {peerId} for ObjectId: {packet.ObjectId.ItemId}, FurnaceId: {packet.FurnaceId}");

        // 용광로 작업 요청 이벤트 발생
        int furnaceId = packet.FurnaceId;
        ulong objectId = packet.ObjectId.ItemId;
        OnPeerSmeltRequestEvent?.Invoke(furnaceId, objectId);
    }

    public void HandlePeerFurnaceRetrieve(int peerId, byte[] data)
    {
        var packet = C_FURNACE_RETRIEVE.Parser.ParseFrom(data);
        Debug.Log($"Received furnace retrieve request from Peer {peerId} for FurnaceId: {packet.FurnaceId}");

        // 용광로 아이템 회수 요청 이벤트 발생
        int furnaceId = packet.FurnaceId;
        OnPeerFurnaceRetrieveEvent?.Invoke(furnaceId);
    }

    private void HandlePeerObjectSpawn(int peerId, byte[] data)
    {
        C_OBJECT_SPAWN packet = C_OBJECT_SPAWN.Parser.ParseFrom(data);
        OnPeerObjectSpawnEvent?.Invoke(peerId, packet);

        // 다른 피어들에게 릴레이
        S_OBJECT_SPAWN relay = new S_OBJECT_SPAWN
        {
            ItemId = 0, // 호스트가 itemId 부여 후 브로드캐스트하므로 여기선 0
            ItemStringKey = packet.ItemStringKey,
            Pos = packet.Pos?.Clone(),
            Rot = packet.Rot?.Clone()
        };
        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_SPAWN, relay, includeSender: false);
    }

    private void HandlePeerObjectDestroy(int peerId, byte[] data)
    {
        C_OBJECT_DESTROY packet = C_OBJECT_DESTROY.Parser.ParseFrom(data);
        OnPeerObjectDestroyEvent?.Invoke(peerId, packet);

        // 다른 피어들에게 릴레이
        S_OBJECT_DESTROY relay = new S_OBJECT_DESTROY { ItemId = packet.ItemId };
        HostNetManager.Instance.BroadcastToPeers(peerId, PacketId.PKT_S_OBJECT_DESTORY, relay, includeSender: false);
    }
}

