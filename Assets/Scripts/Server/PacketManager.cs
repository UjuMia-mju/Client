using UnityEngine;
using Google.Protobuf;
using Protocol;
using UnityEngine.SceneManagement;
using System;

public class PacketManager : Singleton<PacketManager>
{
    // 이벤트
    public event Action<S_LOGIN> OnLoginResultEvent;
    public event Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event Action<S_PLAYER_LEAVE> OnPlayerLeaveEvent;
    public event Action<S_MOVE> OnMoveEvent;
    public event Action<S_CHAT> OnChatEvent;
    public event Action<S_SHOW_STAGE> OnShowStageEvent;

    // 방
    public event Action<S_CREATE_ROOM> OnCreateRoomEvent;
    public event Action<S_ROOM_LIST> OnRoomListEvent;
    public event Action<S_ENTER_ROOM> OnEnterRoomEvent;
    public event Action<S_LEAVE_ROOM> OnLeaveRoomEvent;
    public event Action<S_ROOM_MEMBER_ENTER> OnRoomMemberEnterEvent;
    public event Action<S_ROOM_MEMBER_LEAVE> OnRoomMemberLeaveEvent;

    // 초대
    public event Action<S_INVITE_PLAYER> OnInvitePlayerResultEvent;
    public event Action<S_INVITE_NOTIFICATION> OnInviteNotificationEvent;
    public event Action<S_INVITE_RESPONSE> OnInviteResponseResultEvent;

    // 준비 / 시작
    public event Action<S_READY> OnReadyEvent;
    public event Action<S_START_ROOM> OnStartRoomEvent;
    
    // 스테이지
    
    public event Action<S_START_STAGE> OnStartStageEvent;
    public event Action<S_GET_CLEAR_INFO> OnGetClearInfoEvent;


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
            case PacketId.PKT_S_SHOW_STAGE:
                HandleShowStage(data);
                break;
            case PacketId.PKT_S_CREATE_ROOM:
                HandleCreateRoom(data);
                break;
            case PacketId.PKT_S_ROOM_LIST:
                HandleRoomList(data);
                break;
            case PacketId.PKT_S_ENTER_ROOM:
                HandleEnterRoom(data);
                break;
            case PacketId.PKT_S_LEAVE_ROOM:
                HandleLeaveRoom(data);
                break;
            case PacketId.PKT_S_INVITE_PLAYER:
                HandleInvitePlayerResult(data);
                break;
            case PacketId.PKT_S_INVITE_NOTIFICATION:
                HandleInviteNotification(data);
                break;
            case PacketId.PKT_S_INVITE_RESPONSE:
                HandleInviteResponseResult(data);
                break;
            case PacketId.PKT_S_ROOM_MEMBER_ENTER:
                HandleRoomMemberEnter(data);
                break;
            case PacketId.PKT_S_ROOM_MEMBER_LEAVE:
                HandleRoomMemberLeave(data);
                break;
            case PacketId.PKT_S_READY:
                HandleReady(data);
                break;
            case PacketId.PKT_S_START_ROOM:
                HandleStartRoom(data);
                break;
            case PacketId.PKT_S_START_STAGE:
                HandleStartStage(data);
                break;
            case PacketId.PKT_S_GET_CLEAR_INFO: 
                HandleGetClearInfo(data);
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }


    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data); 

        if (result.Success)
        {
            Debug.Log($"✓ Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}, Name: {result.Player.Name}");
            if (result.PlayerInfo != null)
                Debug.Log($"  PlayerInfo: coin={result.PlayerInfo.Coin}, gem={result.PlayerInfo.Gem}, owned_skins={result.PlayerInfo.OwnedSkins.Count}");

            OnLoginResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data); 

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
    
    private void HandleShowStage(byte[] payloadData)
    {
        S_SHOW_STAGE packet = S_SHOW_STAGE.Parser.ParseFrom(payloadData);
        
        OnShowStageEvent?.Invoke(packet);
    }
    
    private void HandleStartStage(byte[] payloadData)
    {
        S_START_STAGE packet = S_START_STAGE.Parser.ParseFrom(payloadData);
        Debug.Log($"[PacketManager] S_START_STAGE 수신! Success: {packet.Success}");
        OnStartStageEvent?.Invoke(packet);
    }
    
    private void HandleGetClearInfo(byte[] payloadData)
    {
        S_GET_CLEAR_INFO packet = S_GET_CLEAR_INFO.Parser.ParseFrom(payloadData);
        Debug.Log($"[PacketManager] S_GET_CLEAR_INFO 수신! 클리어 데이터 개수: {packet.StageClears.Count}");
        OnGetClearInfoEvent?.Invoke(packet);
    }

    #region Room Management
    private void HandleCreateRoom(byte[] payloadData)
    {
        S_CREATE_ROOM packet = S_CREATE_ROOM.Parser.ParseFrom(payloadData);
        OnCreateRoomEvent?.Invoke(packet);
    }

    private void HandleRoomList(byte[] payloadData)
    {
        S_ROOM_LIST packet = S_ROOM_LIST.Parser.ParseFrom(payloadData);
        OnRoomListEvent?.Invoke(packet);
    }

    private void HandleEnterRoom(byte[] payloadData)
    {
        S_ENTER_ROOM packet = S_ENTER_ROOM.Parser.ParseFrom(payloadData);
        OnEnterRoomEvent?.Invoke(packet);
    }

    private void HandleLeaveRoom(byte[] payloadData)
    {
        S_LEAVE_ROOM packet = S_LEAVE_ROOM.Parser.ParseFrom(payloadData);
        OnLeaveRoomEvent?.Invoke(packet);
    }

    private void HandleRoomMemberEnter(byte[] payloadData)
    {
        S_ROOM_MEMBER_ENTER packet = S_ROOM_MEMBER_ENTER.Parser.ParseFrom(payloadData);
        OnRoomMemberEnterEvent?.Invoke(packet);
    }

    private void HandleRoomMemberLeave(byte[] payloadData)
    {
        S_ROOM_MEMBER_LEAVE packet = S_ROOM_MEMBER_LEAVE.Parser.ParseFrom(payloadData);
        OnRoomMemberLeaveEvent?.Invoke(packet);
    }

    // ========== 초대 ==========
    private void HandleInvitePlayerResult(byte[] payloadData)
    {
        S_INVITE_PLAYER packet = S_INVITE_PLAYER.Parser.ParseFrom(payloadData);
        OnInvitePlayerResultEvent?.Invoke(packet);
    }

    private void HandleInviteNotification(byte[] payloadData)
    {
        S_INVITE_NOTIFICATION packet = S_INVITE_NOTIFICATION.Parser.ParseFrom(payloadData);
        OnInviteNotificationEvent?.Invoke(packet);
    }

    private void HandleInviteResponseResult(byte[] payloadData)
    {
        S_INVITE_RESPONSE packet = S_INVITE_RESPONSE.Parser.ParseFrom(payloadData);
        OnInviteResponseResultEvent?.Invoke(packet);
    }

    // ========== 준비 / 시작 ==========
    private void HandleReady(byte[] payloadData)
    {
        S_READY packet = S_READY.Parser.ParseFrom(payloadData);
        OnReadyEvent?.Invoke(packet);
    }

    private void HandleStartRoom(byte[] payloadData)
    {
        S_START_ROOM packet = S_START_ROOM.Parser.ParseFrom(payloadData);
        OnStartRoomEvent?.Invoke(packet);
    }
    #endregion
}