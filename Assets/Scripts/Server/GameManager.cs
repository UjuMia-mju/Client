using System;
using UnityEngine;
using Protocol;

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