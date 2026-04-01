using System;
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

            // HACK : 서버에 호스트가 2명이 생길수는 없으므로 일단 임시 숫자인 99로 대체합니다. 추후 수정합니다.
            case PacketId.PKT_S_MOVE:
                HandleMove(data);
                break;
            case PacketId.PKT_S_PLAYER_ANIMATION:
                HandleAnimation(data);
                break;
            //case PacketId.PKT_S_PLAYER_STAT:
            //    HandleStat(data);
            //    break;
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
        }
    }

    private void HandleServerPlayerEnter(byte[] payloadData)
    {
        S_PLAYER_ENTER packet = S_PLAYER_ENTER.Parser.ParseFrom(payloadData);
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

    //private void HandleStat(byte[] payloadData)
    //{
    //    S_PLAYER_STAT packet = S_PLAYER_STAT.Parser.ParseFrom(payloadData);
    //    OnStatEvent?.Invoke(packet);
    //}

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
}
