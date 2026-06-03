using System;
using UnityEngine;
using Protocol;

/// <summary>
/// Main 씬에서 멀티플레이 버튼으로 SendCreateRoom() 후,
/// S_CREATE_ROOM 성공 시 서버가 이미 방 입장 처리하므로 C_ENTER_ROOM은 보내지 않는다.
/// 로비 씬 로드 전 S_ENTER_ROOM 이벤트 유실 대비 합성 캐시만 남긴다.
/// </summary>
public class MainMultiPlayHandler : MonoBehaviour
{
    /// <summary>서버에 남은 방이 있을 때: 퇴장 후 방 생성 1회 재시도</summary>
    private bool _pendingCreateAfterLeave;

    private void OnEnable()
    {
        PacketHandler.Instance.OnCreateRoomEvent += OnCreateRoomResult;
        PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoomForCreateRetry;
    }

    private void OnDisable()
    {
        _pendingCreateAfterLeave = false;
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnCreateRoomEvent -= OnCreateRoomResult;
            PacketHandler.Instance.OnLeaveRoomEvent -= OnLeaveRoomForCreateRetry;
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
            return;
        }

        Debug.Log("[MainMultiPlayHandler] 이전 방에서 퇴장했으므로 방 생성을 다시 요청합니다.");
        PacketDispatcher.Instance.SendCreateRoom();
    }

    private void OnCreateRoomResult(S_CREATE_ROOM packet)
    {
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

        // 로비 씬 로드 타이밍 때문에 S_ENTER_ROOM 이벤트를 놓칠 수 있어,
        // 최소 1명(본인) 상태는 캐시로 보장해 둔다. (LobbyRoomClient가 씬 로드 후 적용)
        var synthetic = new S_ENTER_ROOM
        {
            Success = true,
            Room = packet.Room
        };
        synthetic.Members.Add(new RoomMemberInfo
        {
            Player = new Protocol.Player
            {
                Id = (int)NetManager.Instance._playerId,
                Name = NetManager.Instance.PlayerName ?? "",
                Tag = NetManager.Instance.PlayerTag
            },
            IsReady = false
        });
        PacketHandler.SetCachedEnterRoom(synthetic);

        SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
    }
}
