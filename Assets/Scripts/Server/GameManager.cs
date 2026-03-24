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
            NetManager.Instance._playerId = (int)packet.Player.Id; //  이런 캐스팅 부분 나중에 수정해야함.
            SceneManager.LoadScene("StageSelect");  // 로그인 성공 시 게임 씬으로 이동
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }
}