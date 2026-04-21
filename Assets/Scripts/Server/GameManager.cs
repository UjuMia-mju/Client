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
            NetManager.Instance._playerId = (ulong)packet.Player.Id; //  이런 캐스팅 부분 나중에 수정해야함.
            DbCacheManager.Instance.RequestDbData();
            SceneManager.LoadScene(Define.Scene.MAIN);
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