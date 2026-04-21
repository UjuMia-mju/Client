using UnityEngine;
using Google.Protobuf;
using Protocol;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 서버에서 클라이언트로부터 받은 패킷을 처리하는 클래스
/// </summary>
public class PacketHandler : Singleton<PacketHandler>
{
    /// <summary>로비 씬 로드 전에 S_ENTER_ROOM이 도착한 경우를 위해 캐시. LobbyRoomClient가 씬 로드 후 적용.</summary>
    private static S_ENTER_ROOM _cachedEnterRoom;

    // 이벤트
    public event Action<S_LOGIN> OnLoginResultEvent;
    public event Action<S_GACHA> OnGachaResultEvent;
    public event Action<S_GACHA_POOL_LIST> OnGachaPoolListEvent;
    public event Action<S_MY_SKINS> OnMySkinsEvent;
    public event Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event Action<S_PLAYER_LEAVE> OnPlayerLeaveEvent;
    public event Action<S_CREATE_ROOM> OnCreateRoomEvent;
    public event Action<S_ROOM_LIST> OnRoomListEvent;
    public event Action<S_ENTER_ROOM> OnEnterRoomEvent;
    public event Action<S_LEAVE_ROOM> OnLeaveRoomEvent;
    public event Action<S_ROOM_MEMBER_ENTER> OnRoomMemberEnterEvent;
    public event Action<S_ROOM_MEMBER_LEAVE> OnRoomMemberLeaveEvent;
    public event Action<S_INVITE_PLAYER> OnInvitePlayerResultEvent;
    public event Action<S_INVITE_NOTIFICATION> OnInviteNotificationEvent;
    public event Action<S_INVITE_RESPONSE> OnInviteResponseResultEvent;
    public event Action<S_READY> OnReadyEvent;
    public event Action<S_START_ROOM> OnStartRoomEvent;
    public event Action<S_OBJECT_MOVE> OnItemMoveEvent;

    public void HandlePacket(PacketId packetId, byte[] data)
    {
        switch (packetId)
        {
            case PacketId.PKT_S_LOGIN:
                HandleLoginResult(data);
                break;
            case PacketId.PKT_S_RELAY_PACKET:
                RelayPacketHandler.Instance.HandleRelayPacket(S_RELAY_PACKET.Parser.ParseFrom(data));
                break;
            case PacketId.PKT_S_ENTER_GAME:
                HandleEnterGameResult(data);
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
            case PacketId.PKT_S_ROOM_MEMBER_ENTER:
                HandleRoomMemberEnter(data);
                break;
            case PacketId.PKT_S_ROOM_MEMBER_LEAVE:
                HandleRoomMemberLeave(data);
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
            case PacketId.PKT_S_READY:
                HandleReady(data);
                break;
            case PacketId.PKT_S_START_ROOM:
                HandleStartRoom(data);
                break;
            case PacketId.PKT_S_OBJECT_MOVE:
                HandleObjectMove(data);
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }

    public void OnDisconnected()
    {
        Debug.Log("서버와의 연결이 해제되었습니다.");
        // TODO: UI 갱신, 재접속 안내, 게임 상태 초기화 등 필요한 작업 추가
    }

    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data);  // ← S_LOGIN 사용
        
        if (result.Success)
        {
            Debug.Log($"  Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}");
            Debug.Log($"  Player Name: {result.Player.Name}");

            OnLoginResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data);  // ← S_ENTER_GAME 사용

        if (result.Success)
        {
            Debug.Log(" Entered Game Successfully!");
            OnEnterGameResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError(" Failed to Enter Game!");
        }
    }

    private void HandleGachaResult(byte[] data)
    {
        S_GACHA result = S_GACHA.Parser.ParseFrom(data);
        OnGachaResultEvent?.Invoke(result);
    }

    private void HandleGachaPoolList(byte[] data)
    {
        S_GACHA_POOL_LIST result = S_GACHA_POOL_LIST.Parser.ParseFrom(data);
        OnGachaPoolListEvent?.Invoke(result);
    }

    private void HandleMySkins(byte[] data)
    {
        S_MY_SKINS result = S_MY_SKINS.Parser.ParseFrom(data);
        OnMySkinsEvent?.Invoke(result);
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
        if (packet.Success)
            _cachedEnterRoom = packet;
        OnEnterRoomEvent?.Invoke(packet);
    }

    /// <summary>캐시된 S_ENTER_ROOM(성공)을 반환하고 캐시를 비웁니다. 로비 씬 로드 후 한 번만 호출.</summary>
    public static S_ENTER_ROOM GetAndClearCachedEnterRoom()
    {
        var p = _cachedEnterRoom;
        _cachedEnterRoom = null;
        return p;
    }

    /// <summary>방 생성 직후 클라에서 S_ENTER_ROOM을 보내지 않을 때, 가짜 S_ENTER_ROOM을 캐시해 두기 위해 사용.</summary>
    public static void SetCachedEnterRoom(S_ENTER_ROOM packet)
    {
        _cachedEnterRoom = packet;
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

    private void HandleObjectMove(byte[] payloadData)
    {
        S_OBJECT_MOVE packet = S_OBJECT_MOVE.Parser.ParseFrom(payloadData);
        OnItemMoveEvent?.Invoke(packet);
    }

}
