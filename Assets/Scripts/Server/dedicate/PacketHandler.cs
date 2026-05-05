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
    public event Action<S_SKIN_LIST> OnSkinListEvent;
    public event Action<S_MY_SKINS> OnMySkinsEvent;
    public event Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
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
    public event Action<S_STAGE_INFO> OnStageInfoEvent;
    public event Action<S_START_STAGE> OnStartStageEvent;
    public event Action<S_GET_CLEAR_INFO> OnGetClearInfoEvent;
    public event Action<S_GAME_READY_TO_START> OnGameReadyToStartEvent;
    public event Action<S_HOST_SHOW_STAGE> OnHostShowStageEvent;

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
            case PacketId.PKT_S_GACHA:
                HandleGachaResult(data);
                break;
            case PacketId.PKT_S_GACHA_POOL_LIST:
                HandleGachaPoolList(data);
                break;
            case PacketId.PKT_S_SKIN_LIST:
                HandleSkinList(data);
                break;
            case PacketId.PKT_S_MY_SKINS:
                HandleMySkins(data);
                break;
            case PacketId.PKT_S_ENTER_GAME:
                HandleEnterGameResult(data);
                break;
            case PacketId.PKT_C_ENTER_GAME:
                // 일부 서버가 입장 응답에 1045(C_ENTER_GAME) ID를 쓰는 경우 — 페이로드는 S_ENTER_GAME인 경우가 많음
                HandleEnterGamePossiblyWrongPacketId(data);
                break;
            case PacketId.PKT_S_PLAYER_LIST:
                HandlePlayerList(data);
                break;
            case PacketId.PKT_S_PLAYER_ENTER:
                HandlePlayerEnter(data);
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
            case PacketId.PKT_S_STAGE_INFO:
                HandleStageInfo(data);
                break;
            case PacketId.PKT_S_START_STAGE:
                HandleStartStage(data);
                break;
            case PacketId.PKT_S_GET_CLEAR_INFO:
                HandleGetClearInfo(data);
                break;
            case PacketId.PKT_S_GAME_READY_TO_START:
                HandleGameReadyToStart(data);
                break;
            case PacketId.PKT_S_HOST_SHOW_STAGE:
                HandleHostShowStage(data);
                break;
            case PacketId.PKT_C_GET_DB_DATA:
                // 일부 서버가 C_GET_DB_DATA(1000) ID로 S_STAGE_INFO 응답을 보내는 경우
                TryIngestSStageInfoPayloadAsWrongPacketId(packetId, data);
                break;
            default:
                // 알 수 없는 ID지만 페이로드가 S_STAGE_INFO인 경우(잘못된 패킷 ID)
                if (TryIngestSStageInfoPayloadAsWrongPacketId(packetId, data))
                    break;
                Debug.LogWarning(
                    $"[PacketHandler] Unhandled packet ID: {packetId} ({(ushort)packetId}), payloadLen={data?.Length ?? 0}. " +
                    "S_STAGE_INFO는 1036으로 보내는 것이 맞습니다.");
                break;
        }
    }

    public void OnDisconnected()
    {
        Debug.Log("서버와의 연결이 해제되었습니다.");
    }

    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data);
        
        if (result.Success)
        {
            Debug.Log($"  Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}");
            Debug.Log($"  Player Name: {result.Player.Name}");

            // 1) GameManager 등에서 _playerId·DB요청·UI 갱신
            OnLoginResultEvent?.Invoke(result);

            // 2) 씬 전환은 항상 여기서 한 번만 (구독 누락/GameManager 파괴/코루틴 끊김 방지; SceneLoader는 DDOL)
            string active = SceneManager.GetActiveScene().name;
            if (active != Define.Scene.MAIN && active != Define.Scene.GAME_1_1)
            {
                Debug.Log($"[PacketHandler] 로그인 성공 → 씬 전환: {active} → {Define.Scene.MAIN}");
                SceneLoader.Instance.LoadScene(Define.Scene.MAIN);
            }
        }
        else
        {
            Debug.LogError("Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data);

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

    private void HandleEnterGamePossiblyWrongPacketId(byte[] data)
    {
        S_ENTER_GAME s;
        try
        {
            s = S_ENTER_GAME.Parser.ParseFrom(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PacketHandler] PKT_C_ENTER_GAME(1045) S_ENTER_GAME 파싱 실패: {e.Message}");
            return;
        }

        Debug.LogWarning(
            "[PacketHandler] PKT_C_ENTER_GAME(1045) ID로 응답이 왔습니다. 서버는 PKT_S_ENTER_GAME(1046)를 쓰는 것이 맞습니다.");

        if (s.Success)
        {
            Debug.Log(" Entered Game Successfully!");
            OnEnterGameResultEvent?.Invoke(s);
            return;
        }

        // C_ENTER_GAME(및 C_TEST_ENTER_GAME)과 S_ENTER_GAME이 모두 field 1을 쓰면서, PlayerIndex(0)가
        // S.success=false와 동일한 바이트(08 00)로 읽힌다. playerIndex==로컬 ID면 S 실패로 오인한 것으로 처리.
        C_ENTER_GAME c = C_ENTER_GAME.Parser.ParseFrom(data);
        if (c.PlayerIndex == NetManager.Instance._playerId)
        {
            Debug.Log(
                "[PacketHandler] 1045 응답: field-1이 playerIndex(로컬과 일치)로 보입니다. 입장 성공으로 처리합니다.");
            var ok = new S_ENTER_GAME { Success = true };
            OnEnterGameResultEvent?.Invoke(ok);
            return;
        }

        Debug.LogError(" Failed to Enter Game!");
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

    private void HandleSkinList(byte[] data)
    {
        S_SKIN_LIST result = S_SKIN_LIST.Parser.ParseFrom(data);
        OnSkinListEvent?.Invoke(result);
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

        // 캐시뿐 아니라 일반 구독자(RoomMembershipTracker 등)에게도 즉시 전파.
        // LobbyRoomClient는 캐시를 GetAndClearCachedEnterRoom으로 별도 소비하므로 중복 처리되지 않음.
        if (Instance != null)
            Instance.OnEnterRoomEvent?.Invoke(packet);
    }

    /// <summary>캐시를 비우지 않고 들여다보기만. 트래커 등 보조 구독자가 부팅 시 초기 상태를 잡기 위함.</summary>
    public static S_ENTER_ROOM PeekCachedEnterRoom()
    {
        return _cachedEnterRoom;
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

    private void HandleStageInfo(byte[] payloadData)
    {
        S_STAGE_INFO packet = S_STAGE_INFO.Parser.ParseFrom(payloadData);
        ApplySStageInfo(packet, sourceWasWrongPacketId: false, wrongId: 0);
    }

    private void ApplySStageInfo(S_STAGE_INFO packet, bool sourceWasWrongPacketId, ushort wrongId)
    {
        int count = packet?.Stages?.Count ?? 0;
        if (!sourceWasWrongPacketId)
        {
            Debug.Log($"[PacketHandler] S_STAGE_INFO(1036) 수신, 스테이지 {count}개 — DbCacheManager에 반영");
        }
        else
        {
            Debug.LogWarning(
                "[PacketHandler] S_STAGE_INFO 페이로드가 잘못된 패킷 ID(" + wrongId +
                ")로 왔습니다. 서버는 PKT_S_STAGE_INFO(1036)를 쓰는 것이 맞습니다. " +
                $"스테이지 {count}개(적용함)");
        }

        DbCacheManager.CacheStageInfos(packet.Stages);
        OnStageInfoEvent?.Invoke(packet);
    }

    /// <summary>
    /// S_STAGE_INFO 본문인데 1036이 아닌 ID로 온 경우(서버/프로토 불일치) 캐시에 반영.
    /// Stages가 비어 있으면 본문이 S_STAGE_INFO가 아닐 수 있어 무시(다른 메시지 오인 방지).
    /// </summary>
    private bool TryIngestSStageInfoPayloadAsWrongPacketId(PacketId sourcePacketId, byte[] data)
    {
        if (sourcePacketId == PacketId.PKT_S_STAGE_INFO || data == null || data.Length == 0)
            return false;

        S_STAGE_INFO packet;
        try
        {
            packet = S_STAGE_INFO.Parser.ParseFrom(data);
        }
        catch
        {
            return false;
        }

        if (packet == null || packet.Stages == null || packet.Stages.Count == 0)
            return false;

        ApplySStageInfo(packet, true, (ushort)sourcePacketId);
        return true;
    }

    private void HandleStartStage(byte[] payloadData)
    {
        S_START_STAGE packet = S_START_STAGE.Parser.ParseFrom(payloadData);
        OnStartStageEvent?.Invoke(packet);
    }

    private void HandleGetClearInfo(byte[] payloadData)
    {
        S_GET_CLEAR_INFO packet = S_GET_CLEAR_INFO.Parser.ParseFrom(payloadData);
        Debug.Log($"[PacketHandler] S_GET_CLEAR_INFO 수신! Success: {packet.Success}, 클리어 데이터 개수: {packet.StageClears.Count}");
        OnGetClearInfoEvent?.Invoke(packet);
    }
    
    private void HandleGameReadyToStart(byte[] payloadData)
    {
        S_GAME_READY_TO_START packet = S_GAME_READY_TO_START.Parser.ParseFrom(payloadData);
        OnGameReadyToStartEvent?.Invoke(packet);
    }

    private void HandleHostShowStage(byte[] payloadData)
    {
        S_HOST_SHOW_STAGE packet = S_HOST_SHOW_STAGE.Parser.ParseFrom(payloadData);
        OnHostShowStageEvent?.Invoke(packet);
    }

}
