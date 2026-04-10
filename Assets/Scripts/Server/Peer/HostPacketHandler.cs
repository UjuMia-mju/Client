using System;
using NUnit.Framework.Internal;
using Protocol;
using UnityEngine;


/// <summary>
/// 클라이언트가 호스트로부터 받은 패킷을 처리하는 클래스
/// </summary>
public class HostPacketHandler : Singleton<HostPacketHandler>
{
    public event Action<ulong, S_MOVE> OnMoveEvent;
    public event Action<S_CHAT> OnChatEvent;
    public event Action<ulong, S_PLAYER_ANIMATION> OnAnimationEvent;
    public event Action<S_PLAYER_STAT> OnStatEvent;
    public event Action<ulong, S_OBJECT_PICKUP> OnItemAttached;
    public event Action<ulong, S_OBJECT_DROP> OnItemDetatched;
    public event Action<S_OBJECT_MOVE> OnItemMoveEvent;  
    public event Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event Action<S_OBJECT_SMELT> OnSmeltEvent;
    public event Action<S_SMELT_COMPLETE> OnSmeltCompleteEvent;
    public event Action<S_FURNACE_RETRIEVE> OnFurnaceRetrieveEvent;
    public event Action<S_OBJECT_SPAWN> OnObjectSpawnEvent;
    public event Action<S_OBJECT_DESTROY> OnObjectDestroyEvent;
    public event Action<S_SPACESHIP_UPDATE> OnSpaceshipUpdateEvent;
    public event Action<S_SPACESHIP_COMPLETE> OnSpaceshipCompleteEvent;
    public event Action<S_TIMER_SYNC> OnTimerSyncEvent;

    public void HandlePacket(PacketId packetId, byte[] data)
    {
        switch (packetId)
        {
            case PacketId.PKT_S_PLAYER_ENTER:
                HandleServerPlayerEnter(data);
                break;
            case PacketId.PKT_S_CHAT:
                HandleChat(data);
                break;
            case PacketId.PKT_S_MOVE:
                HandleMove(data);
                break;
            case PacketId.PKT_S_PLAYER_ANIMATION:
                HandleAnimation(data);
                break;
            case PacketId.PKT_S_PLAYER_STAT:
                HandleStat(data);
                break;
            case PacketId.PKT_S_OBJECT_PICKUP:
                HandleItemAttached(data);
                break;
            case PacketId.PKT_S_OBJECT_DROP:
                HandleItemDetatched(data);
                break;
            case PacketId.PKT_S_OBJECT_MOVE:
                HandleItemMove(data);
                break;
                //case PacketId.PKT_S_WORKBENCH:
                //    HandleCraftTable(data);
                //    break;
            case PacketId.PKT_S_OBJECT_SMELT:
                HandleSmelt(data);
                break;
            case PacketId.PKT_S_SMELT_COMPLETE:
                HandleSmeltComplete(data);
                break;
            case PacketId.PKT_S_FURNACE_RETRIEVE:
                HandleFurnaceRetrieve(data);
                break;
            case PacketId.PKT_S_OBJECT_SPAWN:
                HandleObjectSpawn(data);
                break;
            case PacketId.PKT_S_OBJECT_DESTORY:
                HandleObjectDestroy(data);
                break;
            case PacketId.PKT_S_SPACESHIP_UPDATE:
                HandleSpaceshipUpdate(data);
                break;
            case PacketId.PKT_S_SPACESHIP_COMPLETE:
                HandleSpaceshipComplete(data);
                break;
            case PacketId.PKT_S_TIMER_SYNC:
                HandleTimerSync(data);
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }

    private void HandleServerPlayerEnter(byte[] payloadData)
    {
        S_PLAYER_ENTER packet = S_PLAYER_ENTER.Parser.ParseFrom(payloadData);

        // 첫 번째로 받는 패킷은 반드시 피어 자신의 정보 (호스트가 가장 먼저 전송)
        // _playerId가 0이면 아직 할당 전이므로 자신의 ID로 갱신하고 스폰은 하지 않음
        if (NetManager.Instance._playerId == 0)
        {
            NetManager.Instance._playerId = (ulong)packet.Player.PlayerId;
            Debug.Log($"[HostPacketHandler] Assigned local PlayerId: {NetManager.Instance._playerId}");
            // 자기 자신은 RemotePlayer로 스폰하지 않으므로 이벤트를 올리지 않음
            return;
        }

        Debug.Log($"[HostPacketHandler] Received S_PLAYER_ENTER for remote player: {packet.Player.PlayerId}");
        OnPlayerEnterEvent?.Invoke(packet);
    }

    private void HandleMove(byte[] payloadData)
    {
        S_MOVE packet = S_MOVE.Parser.ParseFrom(payloadData);
        OnMoveEvent?.Invoke(packet.PlayerId, packet);
    }

    private void HandleChat(byte[] payloadData)
    {
        S_CHAT packet = S_CHAT.Parser.ParseFrom(payloadData);
        OnChatEvent?.Invoke(packet);
    }

    private void HandleAnimation(byte[] payloadData)
    {
        S_PLAYER_ANIMATION packet = S_PLAYER_ANIMATION.Parser.ParseFrom(payloadData);
        OnAnimationEvent?.Invoke(packet.PlayerId, packet);
    }

    private void HandleStat(byte[] payloadData)
    {
       S_PLAYER_STAT packet = S_PLAYER_STAT.Parser.ParseFrom(payloadData);
       //Debug.Log($"Received PlayerStat packet: PlayerId={packet.PlayerId}, Hp={packet.Hp}, Oxygen={packet.Oxygen}");
       PeerStatManager.Instance.UpdateStat(packet.PlayerId, packet.Hp, packet.Oxygen);
       OnStatEvent?.Invoke(packet); // 여기서 받은 데이터 기반으로 UI 업데이트 하면 됨.
    }

    private void HandleItemAttached(byte[] payloadData)
    {
        S_OBJECT_PICKUP packet = S_OBJECT_PICKUP.Parser.ParseFrom(payloadData);
        OnItemAttached?.Invoke(packet.PlayerId, packet);
    }

    private void HandleItemDetatched(byte[] payloadData)
    {
        S_OBJECT_DROP packet = S_OBJECT_DROP.Parser.ParseFrom(payloadData);
        OnItemDetatched?.Invoke(packet.PlayerId, packet);
    }

    private void HandleItemMove(byte[] payloadData)
    {
        S_OBJECT_MOVE packet = S_OBJECT_MOVE.Parser.ParseFrom(payloadData);
        OnItemMoveEvent?.Invoke(packet);
    }

    //private void HandleCraftTable(byte[] payloadData)
    //{
    //    S_WORKBENCH_LIST packet = S_WORKBENCH_LIST.Parser.ParseFrom(payloadData);
    //    OnCraftTableEvent?.Invoke(packet);
    //}

    private void HandleSmelt(byte[] payloadData)
    {
        S_OBJECT_SMELT packet = S_OBJECT_SMELT.Parser.ParseFrom(payloadData);
        OnSmeltEvent?.Invoke(packet);
    }

    private void HandleSmeltComplete(byte[] payloadData)
    {
        S_SMELT_COMPLETE packet = S_SMELT_COMPLETE.Parser.ParseFrom(payloadData);
        OnSmeltCompleteEvent?.Invoke(packet); // 필요 시 완성 알림 처리
    }

    private void HandleFurnaceRetrieve(byte[] payloadData)
    {
        S_FURNACE_RETRIEVE packet = S_FURNACE_RETRIEVE.Parser.ParseFrom(payloadData);
        OnFurnaceRetrieveEvent?.Invoke(packet); // 필요 시 완성 알림 처리
    }

    private void HandleObjectSpawn(byte[] payloadData)
    {
        S_OBJECT_SPAWN packet = S_OBJECT_SPAWN.Parser.ParseFrom(payloadData);
        OnObjectSpawnEvent?.Invoke(packet);
    }

    private void HandleObjectDestroy(byte[] payloadData)
    {
        S_OBJECT_DESTROY packet = S_OBJECT_DESTROY.Parser.ParseFrom(payloadData);
        OnObjectDestroyEvent?.Invoke(packet);
    }


    private void HandleSpaceshipUpdate(byte[] data)
    {
        S_SPACESHIP_UPDATE packet = S_SPACESHIP_UPDATE.Parser.ParseFrom(data);
        OnSpaceshipUpdateEvent?.Invoke(packet);
    }

    private void HandleSpaceshipComplete(byte[] data)
    {
        S_SPACESHIP_COMPLETE packet = S_SPACESHIP_COMPLETE.Parser.ParseFrom(data);
        OnSpaceshipCompleteEvent?.Invoke(packet);
    }

    private void HandleTimerSync(byte[] data)
    {
        S_TIMER_SYNC packet = S_TIMER_SYNC.Parser.ParseFrom(data);
        OnTimerSyncEvent?.Invoke(packet);
    }

}


