using System;
using UnityEngine;
using Protocol;

/// <summary>
/// 로그인·<c>S_STAGE_INFO</c> 등 공용 로직. <b>스테이지 씬의 미션 타이머 HUD</b>는
/// 같은 <c>GameManagers</c> 아래 <see cref="GameRuleManager"/>의 <c>Mission Timer Ui Root</c>에서
/// <see cref="GameplayReadyCoordinator"/> 게이트 해제 후 켜집니다(이 컴포넌트는 DDOL 싱글톤이라 씬 전용 레퍼런스에 부적합).
/// </summary>
public class GameManager : MonoBehaviorSingleton<GameManager>
{
    void OnEnable()
    {
        _ = MessageManager.Instance;
        PacketHandler.Instance.OnLoginResultEvent += OnLoginResult;
        PacketHandler.Instance.OnStageInfoEvent += OnStageInfo;
    }

    void OnDisable()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnLoginResultEvent -= OnLoginResult;
            PacketHandler.Instance.OnStageInfoEvent -= OnStageInfo;
        }
    }

    private void OnLoginResult(S_LOGIN packet)
    {
        if (packet.Success)
        {
            ApplySLoginToLocalSession(packet);

            try
            {
                DbCacheManager.RequestDbData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] DB 요청 실패(씬 전환은 계속): {e.Message}");
            }

            // 씬 전환은 PacketHandler.HandleLoginResult에서 S_LOGIN 직후 SceneLoader로 통일
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
            MessageManager.Instance.ShowLoginFailureAfterServerResponse();
        }
    }

    /// <summary>
    /// 서버는 종종 <see cref="S_LOGIN.Player"/> 없이 <see cref="S_LOGIN.PlayerInfo"/>만 보냅니다.
    /// proto 상 Player는 id/name/tag를 갖지만, 필드가 비어 있으면 로그인 시 입력한 userId로 이름을 보강합니다.
    /// </summary>
    static void ApplySLoginToLocalSession(S_LOGIN packet)
    {
        var nm = NetManager.Instance;
        if (nm == null) return;

        int id = packet.Player != null ? packet.Player.Id : 0;
        string name = packet.Player != null ? packet.Player.Name ?? "" : "";
        int tag = packet.Player != null ? packet.Player.Tag : 0;

        if (id == 0 && packet.PlayerInfo != null && packet.PlayerInfo.PlayerId != 0)
            id = packet.PlayerInfo.PlayerId;

        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(nm.LastAttemptedLoginUserId))
            name = nm.LastAttemptedLoginUserId;

        if (id != 0)
            nm._playerId = (ulong)id;

        nm.SetLocalPlayerProfile(name, tag);
        RoomMemberDisplayCache.Instance?.RefreshLocalMemberFromNetManager();

        Debug.Log(
            $"[GameManager] 로컬 세션 반영: _playerId={nm._playerId}, PlayerName={nm.PlayerName}, PlayerTag={nm.PlayerTag} " +
            $"(S_LOGIN.Player={(packet.Player != null)}, S_LOGIN.PlayerInfo={(packet.PlayerInfo != null)})");
    }

    private void OnStageInfo(S_STAGE_INFO packet)
    {
        Debug.Log($"[GameManager] S_STAGE_INFO 수신: {packet.Stages.Count}개");
    }
}