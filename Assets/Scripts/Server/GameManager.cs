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
            Debug.Log($"✓ Login Success! Player ID: {packet.Player.Id}, Name: {packet.Player.Name}");
            NetManager.Instance._playerId = (ulong)packet.Player.Id;
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
        }
    }

    private void OnStageInfo(S_STAGE_INFO packet)
    {
        Debug.Log($"[GameManager] S_STAGE_INFO 수신: {packet.Stages.Count}개");
    }
}