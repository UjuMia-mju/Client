using UnityEngine;
using Google.Protobuf;
using Protocol;
using System;

/// <summary>
/// 호스트가 피어로부터 받은 패킷을 처리하는 클래스
/// NetManager의 피어 receive 루프에서 호출됨
/// 각 C_ 패킷에 대한 이벤트를 정의하여 게임 로직에서 구독할 수 있도록 함
/// 예: 플레이어 이동, 채팅 메시지, 애니메이션 상태 변경, 아이템 상호작유속 등
/// </summary>
public class PeerPacketHandler : Singleton<PeerPacketHandler>
{
    // 이벤트 (호스트가 피어로부터 받은 C_ 패킷들)
    public event Action<int, C_MOVE> OnPeerMoveEvent;
    public event Action<int, C_CHAT> OnPeerChatEvent;
    public event Action<int, C_PLAYER_ANIMATION> OnPeerAnimationEvent;
    public event Action<int, C_OBJECT_PICKUP> OnPeerItemAttachedEvent;
    public event Action<int, C_OBJECT_DROP> OnPeerItemDetachedEvent;
    public event Action<int, C_ENTER_GAME> OnPeerEnterGameEvent;
    public event Action<int, C_PLAYER_STAT_EVENT> OnPeerStatEvent;
    public event Action<int, ulong> OnPeerSmeltRequestEvent;
    public event Action<int> OnPeerFurnaceRetrieveEvent;
    public event Action<int, C_OBJECT_SPAWN> OnPeerObjectSpawnEvent;
    public event Action<int, C_OBJECT_DESTROY> OnPeerObjectDestroyEvent;
    public event Action<int, C_SPACESHIP_INSERT> OnPeerSpaceshipInsertEvent;
    public event Action<S_GAME_READY_TO_START> OnGameReadyToStartEvent;
    public event Action<int, C_RESOURCE_HIT> OnPeerResourceHitEvent;
    public event Action<int, C_PLAYER_DEAD> OnPeerPlayerDeadEvent;


    /// <summary>
    /// 호스트가 피어로부터 받은 패킷 처리
    /// NetManager의 피어 receive 루프에서 호출됨
    /// </summary>
    public void HandlePeerPacket(int peerId, PacketId packetId, byte[] data)
    {
        //Debug.Log($"[Peer {peerId}] Received packet: {packetId}, Size: {data.Length} bytes");

        switch (packetId)
        {
            case PacketId.PKT_C_ENTER_GAME:             // PKT_C_TEST_ENTER_GAME → PKT_C_ENTER_GAME
                Debug.Log($"[Peer {peerId}] Handling C_ENTER_GAME packet");
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
            case PacketId.PKT_C_SPACESHIP_INSERT:
                HandlePeerSpaceshipInsert(peerId, data);
                break;
            case PacketId.PKT_S_GAME_READY_TO_START:
                HandleGameReadyToStart(data);
                break;
            case PacketId.PKT_C_RESOURCE_HIT:
                HandlePeerResourceHit(peerId, data);
                break;
            case PacketId.PKT_C_PLAYER_DEAD:
                HandlePeerPlayerDead(peerId, data);
                break;
            default:
                Debug.LogWarning($"[Peer {peerId}] Unhandled packet ID: {packetId}");
                break;
        }
    }

    private void HandlePeerEnterGame(int peerId, byte[] data)
    {
        C_ENTER_GAME packet = C_ENTER_GAME.Parser.ParseFrom(data);

        // 1. 피어 스탯 등록
        HostStatManager.Instance?.AddPlayer((ulong)peerId);

        // 2. S_ENTER_GAME 구성: 호스트 + 기존 피어들 + 신규 피어
        S_ENTER_GAME response = new S_ENTER_GAME { Success = true };

        // 호스트 자신
        response.Players.Add(new PlayerGameInfo
        {
            PlayerId = (int)NetManager.Instance._playerId,
            Name = "Host",
            Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
            Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
        });

        // 기존 접속 중인 피어들
        if (PlayManager.Instance != null)
        {
            foreach (var existingId in PlayManager.Instance._remotePlayers.Keys)
            {
                response.Players.Add(new PlayerGameInfo
                {
                    PlayerId = (int)existingId,
                    Name = "Peer",
                    Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                    Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
                });
            }
        }

        // 신규 피어
        response.Players.Add(new PlayerGameInfo
        {
            PlayerId = peerId,
            Name = "Peer",
            Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
            Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
        });

        // 3. 전체 피어에게 브로드캐스트 (신규 피어 포함)
        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_ENTER_GAME, response);
        Debug.Log($"[PeerPacketHandler] S_ENTER_GAME 브로드캐스트. players={response.Players.Count}");

        // 4. 호스트 측 PlayManager 스폰 이벤트
        OnPeerEnterGameEvent?.Invoke(peerId,    packet);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_MOVE, relay);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_CHAT, relay);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_PLAYER_ANIMATION, relay);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_OBJECT_PICKUP, relay);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_OBJECT_DROP, relay);
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

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_OBJECT_MOVE, relay);
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

        if (statManager.TryGetPlayerStat(packet.TargetPlayerId, out var stat))
        {
            var syncPacket = new S_PLAYER_STAT
            {
                PlayerId = packet.TargetPlayerId,
                Hp = stat.GetHp(),
                Oxygen = stat.GetOxygen()
            };

            PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_PLAYER_STAT, syncPacket);
            OnPeerStatEvent?.Invoke(peerId, packet);
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
        // 브로드캐스트는 PlayManager.OnPeerObjectSpawn에서
        // 호스트가 스폰 후 실제 ID로 처리 (itemId=0 릴레이 제거)

        // 다른 피어들에게 릴레이

        // 확인 필요
        // S_OBJECT_SPAWN relay = new S_OBJECT_SPAWN
        // {
        //     ItemId = 0, // 호스트가 itemId 부여 후 브로드캐스트하므로 여기선 0
        //     ItemStringKey = packet.ItemStringKey,
        //     Pos = packet.Pos?.Clone(),
        //     Rot = packet.Rot?.Clone()
        // };

        // PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_OBJECT_SPAWN, relay);
    }

    private void HandlePeerObjectDestroy(int peerId, byte[] data)
    {
        C_OBJECT_DESTROY packet = C_OBJECT_DESTROY.Parser.ParseFrom(data);

        // 호스트에서 아이템 존재 검증
        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null)
        {
            Debug.LogWarning($"[PeerObjectDestroy] 존재하지 않는 아이템 삭제 요청 무시: id={packet.ItemId}");
            return;
        }

        OnPeerObjectDestroyEvent?.Invoke(peerId, packet);
        S_OBJECT_DESTROY relay = new S_OBJECT_DESTROY { ItemId = packet.ItemId };

        PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_OBJECT_DESTORY, relay);
    }


    private void HandlePeerSpaceshipInsert(int peerId, byte[] data)
    {
        C_SPACESHIP_INSERT packet = C_SPACESHIP_INSERT.Parser.ParseFrom(data);
        OnPeerSpaceshipInsertEvent?.Invoke(peerId, packet);
    }

    private void HandleGameReadyToStart(byte[] data)
    {
        S_GAME_READY_TO_START packet = S_GAME_READY_TO_START.Parser.ParseFrom(data);
        OnGameReadyToStartEvent?.Invoke(packet);
    }

    private void HandlePeerResourceHit(int peerId, byte[] data)
    {
        C_RESOURCE_HIT packet = C_RESOURCE_HIT.Parser.ParseFrom(data);
        Debug.Log($"[PeerPacketHandler] PeerResourceHit: peerId={peerId}, resourceId={packet.ResourceId}");
        OnPeerResourceHitEvent?.Invoke(peerId, packet);
    }

    private void HandlePeerPlayerDead(int peerId, byte[] data)
    {
        C_PLAYER_DEAD packet = C_PLAYER_DEAD.Parser.ParseFrom(data);
        OnPeerPlayerDeadEvent?.Invoke(peerId, packet);
    }
}

