using System.Collections.Generic;
using UnityEngine;
using Protocol;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviorSingleton<GameManager>
{    
    void Start()
    {
        // 패킷 이벤트 구독
        PacketHandler.Instance.OnLoginResultEvent += OnLoginResult;
        PacketHandler.Instance.OnStageInfoEvent += OnStageInfo;
    }

    void OnDestroy()
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
            DbCacheManager.Instance.RequestDbData();

            // 이미 게임 씬에 있으면 씬 전환 하지 않음 (단품 테스트 대응)
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != Define.Scene.MAIN && currentScene != Define.Scene.GAME_1_1)
            {
                SceneManager.LoadScene(Define.Scene.MAIN);
            }
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