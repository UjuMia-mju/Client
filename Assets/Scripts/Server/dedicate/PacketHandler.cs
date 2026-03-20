using UnityEngine;
using Google.Protobuf;
using Protocol;
using UnityEngine.SceneManagement;
using System;

public class PacketHandler : Singleton<PacketHandler>
{
    // 이벤트
    public event Action<S_LOGIN> OnLoginResultEvent;
    public event Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event Action<S_PLAYER_LEAVE> OnPlayerLeaveEvent;
    public event Action<S_MOVE> OnMoveEvent;
    public event Action<S_CHAT> OnChatEvent;
    public event Action<S_PLAYER_ANIMATION> OnAnimationEvent;
    public event Action<S_PLAYER_STAT> OnStatEvent;
    public event Action<S_OBJECT_PICKUP> OnItemAttached;
    public event Action<S_OBJECT_DROP> OnItemDetatched;
    //public event System.Action<S_OBJECT_MOVE> OnItemMoveEvent;
    //public event System.Action<S_WORKBENCH_LIST> OnCraftTableEvent;

    public void HandlePacket(PacketId packetId, byte[] data)
    {
        Debug.Log($"Received packet with ID: {packetId}, Size: {data.Length} bytes");
        switch (packetId)
        {
            case PacketId.PKT_S_LOGIN: 
                HandleLoginResult(data);
                break;
            case PacketId.PKT_S_ENTER_GAME: 
                HandleEnterGameResult(data);
                break;
            case PacketId.PKT_S_CHAT: 
                HandleChat(data);
                break;
            case PacketId.PKT_S_PLAYER_LIST: 
                HandlePlayerList(data);
                break;
            case PacketId.PKT_S_PLAYER_ENTER: 
                HandlePlayerEnter(data);
                break;
            case PacketId.PKT_S_PLAYER_LEAVE: 
                HandlePlayerLeave(data);
                break;
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
            //case PacketId.PKT_S_OBJECT_MOVE:
            //    HandleItemMove(data);
            //    break;
            //case PacketId.PKT_S_WORKBENCH:
            //    HandleCraftTable(data);
            //    break;

            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
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

            OnLoginResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }

    public void OnDisconnected()
    {
        Debug.Log("서버와의 연결이 해제되었습니다.");
        // TODO: UI 갱신, 재접속 안내, 게임 상태 초기화 등 필요한 작업 추가
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data);  // ← S_ENTER_GAME 사용

        if (result.Success)
        {
            Debug.Log("✓ Entered Game Successfully!");
            OnEnterGameResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Failed to Enter Game!");
        }
    }

    private void HandlePlayerList(byte[] payloadData)
    {
        S_PLAYER_LIST packet = S_PLAYER_LIST.Parser.ParseFrom(payloadData);
        OnPlayerListEvent?.Invoke(packet);
    }

    private void HandlePlayerEnter(byte[] payloadData)
    {
        S_PLAYER_ENTER packet = S_PLAYER_ENTER.Parser.ParseFrom(payloadData);
        OnPlayerEnterEvent?.Invoke(packet);
    }

    private void HandlePlayerLeave(byte[] payloadData)
    {
        S_PLAYER_LEAVE packet = S_PLAYER_LEAVE.Parser.ParseFrom(payloadData);
        OnPlayerLeaveEvent?.Invoke(packet);
    }

    private void HandleMove(byte[] payloadData)
    {
        S_MOVE packet = S_MOVE.Parser.ParseFrom(payloadData);
        OnMoveEvent?.Invoke(packet);
    }

    private void HandleChat(byte[] payloadData)
    {
        S_CHAT packet = S_CHAT.Parser.ParseFrom(payloadData);
        OnChatEvent?.Invoke(packet);
    }

    private void HandleAnimation(byte[] payloadData)
    {
        S_PLAYER_ANIMATION packet = S_PLAYER_ANIMATION.Parser.ParseFrom(payloadData);
        OnAnimationEvent?.Invoke(packet);
    }

    //private void HandleStat(byte[] payloadData)
    //{
    //    S_PLAYER_STAT packet = S_PLAYER_STAT.Parser.ParseFrom(payloadData);
    //    OnStatEvent?.Invoke(packet);
    //}

    private void HandleItemAttached(byte[] payloadData)
    {
        S_OBJECT_PICKUP packet = S_OBJECT_PICKUP.Parser.ParseFrom(payloadData);
        OnItemAttached?.Invoke(packet);
    }

    private void HandleItemDetatched(byte[] payloadData)
    {
        S_OBJECT_DROP packet = S_OBJECT_DROP.Parser.ParseFrom(payloadData);
        OnItemDetatched?.Invoke(packet);
    }

    //private void HandleItemMove(byte[] payloadData)
    //{
    //    S_OBJECT_MOVE packet = S_OBJECT_MOVE.Parser.ParseFrom(payloadData);
    //    OnItemMoveEvent?.Invoke(packet);
    //}

    //private void HandleCraftTable(byte[] payloadData)
    //{
    //    S_WORKBENCH_LIST packet = S_WORKBENCH_LIST.Parser.ParseFrom(payloadData);
    //    OnCraftTableEvent?.Invoke(packet);
    //}
}
