using System;
using UnityEngine;
using Protocol;

/// <summary>
/// Main 씬에서 멀티/싱글 방 생성(S_CREATE_ROOM) 및 싱글 시 방 시작(S_START_ROOM) 후 씬 전환.
/// 멀티: 로비 씬. 싱글: 로비 생략 → S_START_ROOM 성공 시 스테이지 선택.
/// </summary>
public class MainMultiPlayHandler : MonoBehaviour
{
    /// <summary>서버에 남은 방이 있을 때: 퇴장 후 방 생성 1회 재시도</summary>
    private bool _pendingCreateAfterLeave;

    private void OnEnable()
    {
        PacketHandler.Instance.OnCreateRoomEvent += OnCreateRoomResult;
        PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoomForCreateRetry;
        PacketHandler.Instance.OnStartRoomEvent += OnStartRoomResult;
    }

    private void OnDisable()
    {
        _pendingCreateAfterLeave = false;
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnCreateRoomEvent -= OnCreateRoomResult;
            PacketHandler.Instance.OnLeaveRoomEvent -= OnLeaveRoomForCreateRetry;
            PacketHandler.Instance.OnStartRoomEvent -= OnStartRoomResult;
        }
    }

    private static bool IsAlreadyInRoomError(string errorMsg)
    {
        if (string.IsNullOrEmpty(errorMsg)) return false;
        return errorMsg.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0
            || errorMsg.IndexOf("이미", StringComparison.Ordinal) >= 0;
    }

    private void OnLeaveRoomForCreateRetry(S_LEAVE_ROOM packet)
    {
        if (!_pendingCreateAfterLeave) return;
        _pendingCreateAfterLeave = false;

        if (!packet.Success)
        {
            MessageManager.Instance?.ShowKey(MessageKeys.CreateRoomFailedLeavePrevious);
            Debug.LogWarning(
                "[MainMultiPlayHandler] 이전 방 퇴장에 실패해 방을 새로 만들 수 없습니다. 로비/메인을 왕복하거나 다시 시도하세요.");
            if (SinglePlaySession.IsAwaitingRoomBootstrap)
                SinglePlaySilentBootstrap.NotifyFailed("이전 방 퇴장 실패");
            return;
        }

        Debug.Log("[MainMultiPlayHandler] 이전 방에서 퇴장했으므로 방 생성을 다시 요청합니다.");
        PacketDispatcher.Instance.SendCreateRoom();
    }

    private void OnCreateRoomResult(S_CREATE_ROOM packet)
    {
        if (SinglePlaySession.IsAwaitingRoomBootstrap)
        {
            OnCreateRoomResultForSinglePlay(packet);
            return;
        }

        if (!packet.Success)
        {
            if (IsAlreadyInRoomError(packet.ErrorMsg) && !_pendingCreateAfterLeave)
            {
                _pendingCreateAfterLeave = true;
                PacketDispatcher.Instance.SendLeaveRoom();
                Debug.Log(
                    "[MainMultiPlayHandler] 서버에 이전 방 세션이 남아 있어 퇴장(C_LEAVE_ROOM) 후 방 생성을 재시도합니다.");
                return;
            }

            string errorMsg = string.IsNullOrWhiteSpace(packet.ErrorMsg) ? string.Empty : packet.ErrorMsg.Trim();
            MessageManager.Instance?.ShowServerError(
                MessageKeys.CreateRoomFailed,
                MessageKeys.CreateRoomFailedWithReason,
                errorMsg);
            Debug.LogWarning($"[MainMultiPlayHandler] 방 생성 실패: {packet.ErrorMsg}");
            return;
        }

        ApplySyntheticEnterRoom(packet.Room, lobbyReadyState: false);
        SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
    }

    private void OnCreateRoomResultForSinglePlay(S_CREATE_ROOM packet)
    {
        if (!packet.Success)
        {
            if (IsAlreadyInRoomError(packet.ErrorMsg) && !_pendingCreateAfterLeave)
            {
                _pendingCreateAfterLeave = true;
                PacketDispatcher.Instance.SendLeaveRoom();
                Debug.Log(
                    "[MainMultiPlayHandler] 싱글: 이전 방 세션 퇴장 후 방 생성 재시도.");
                return;
            }

            string errorMsg = string.IsNullOrWhiteSpace(packet.ErrorMsg) ? string.Empty : packet.ErrorMsg.Trim();
            MessageManager.Instance?.ShowServerError(
                MessageKeys.CreateRoomFailed,
                MessageKeys.CreateRoomFailedWithReason,
                errorMsg);
            Debug.LogWarning($"[MainMultiPlayHandler] 싱글 방 생성 실패: {packet.ErrorMsg}");
            SinglePlaySilentBootstrap.NotifyFailed($"방 생성 실패: {packet.ErrorMsg}");
            return;
        }

        ApplySyntheticEnterRoom(packet.Room, lobbyReadyState: false);
        RoomMembershipTracker.Instance?.EnsureWired();
        ConnectManager.Instance?.SetHostRole(true);

        // 로비 UI 없이 서버에만 레디 true (표시 캐시는 싱글 세션이라 false 유지).
        PacketDispatcher.Instance.SendReady(true);
        PacketDispatcher.Instance.SendStartRoom();
    }

    private void OnStartRoomResult(S_START_ROOM packet)
    {
        if (!SinglePlaySession.IsAwaitingRoomBootstrap)
            return;

        if (!packet.Success)
        {
            MessageManager.TryShowServerError(
                MessageKeys.StartRoomFailed,
                MessageKeys.StartRoomFailedWithReason,
                packet.ErrorMsg);
            Debug.LogWarning($"[MainMultiPlayHandler] 싱글 방 시작 실패: {packet.ErrorMsg}");
            SinglePlaySilentBootstrap.NotifyFailed($"방 시작 실패: {packet.ErrorMsg}");
            return;
        }

        SinglePlaySession.OnRoomBootstrapComplete();
        SinglePlaySilentBootstrap.NotifyEnteringStageSelect();
        SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
    }

    /// <param name="lobbyReadyState">로비·스테이지 선택 UI용 레디. 멀티/싱글 모두 false — 로비 준비 버튼·S_READY만 반영.</param>
    static void ApplySyntheticEnterRoom(RoomInfo room, bool lobbyReadyState)
    {
        var synthetic = new S_ENTER_ROOM
        {
            Success = true,
            Room = room
        };
        synthetic.Members.Add(new RoomMemberInfo
        {
            Player = new Protocol.Player
            {
                Id = (int)NetManager.Instance._playerId,
                Name = NetManager.Instance.PlayerName ?? "",
                Tag = NetManager.Instance.PlayerTag
            },
            IsReady = lobbyReadyState
        });
        PacketHandler.SetCachedEnterRoom(synthetic);
    }
}
