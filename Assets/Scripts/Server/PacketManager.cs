using UnityEngine;
using Google.Protobuf;
using Protocol;
using UnityEngine.SceneManagement;

public class PacketManager : Singleton<PacketManager>
{
    // 이벤트
    public event System.Action<S_LOGIN> OnLoginResultEvent;
    public event System.Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event System.Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event System.Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event System.Action<S_PLAYER_LEAVE> OnPlayerLeaveEvent;
    public event System.Action<S_MOVE> OnMoveEvent;
    public event System.Action<S_CHAT> OnChatEvent;

    // 방
    public event System.Action<S_CREATE_ROOM> OnCreateRoomEvent;
    public event System.Action<S_ROOM_LIST> OnRoomListEvent;
    public event System.Action<S_ENTER_ROOM> OnEnterRoomEvent;
    public event System.Action<S_LEAVE_ROOM> OnLeaveRoomEvent;
    public event System.Action<S_ROOM_MEMBER_ENTER> OnRoomMemberEnterEvent;
    public event System.Action<S_ROOM_MEMBER_LEAVE> OnRoomMemberLeaveEvent;

    // 초대
    public event System.Action<S_INVITE_PLAYER> OnInvitePlayerResultEvent;
    public event System.Action<S_INVITE_NOTIFICATION> OnInviteNotificationEvent;
    public event System.Action<S_INVITE_RESPONSE> OnInviteResponseResultEvent;

    // 준비 / 시작
    public event System.Action<S_READY> OnReadyEvent;
    public event System.Action<S_START_ROOM> OnStartRoomEvent;

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

    // ========== 방 ==========
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
}