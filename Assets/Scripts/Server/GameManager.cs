using System.Collections.Generic;
using UnityEngine;
using Protocol;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviorSingleton<GameManager>
{    
    void Start()
    {
        // 패킷 이벤트 구독
        PacketManager.Instance.OnLoginResultEvent += OnLoginResult;
    }

    void OnDestroy()
    {
        if (PacketManager.Instance != null)
        {
            PacketManager.Instance.OnLoginResultEvent -= OnLoginResult;
        }
    }
    private void OnLoginResult(S_LOGIN packet)
    {
        if (packet.Success)
        {
            Debug.Log($"✓ Login Success! Player ID: {packet.Player.Id}, Name: {packet.Player.Name}");
            NetManager.Instance._playerId = packet.Player.Id;
            NetManager.Instance.PlayerName = packet.Player.Name;
            NetManager.Instance.PlayerTag = packet.Player.Tag;
            NetManager.Instance.PlayerInfo = packet.PlayerInfo;
            // 로그인 성공 → Splash → Main → (멀티플레이 버튼 시 방 생성 후 로비)
            SceneLoader.Instance.LoadScene(Define.Scene.SPLASH);
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }
}
