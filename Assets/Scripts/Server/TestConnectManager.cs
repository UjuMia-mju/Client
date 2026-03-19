using System.Collections;
using UnityEngine;

public class TestConnectManager : SceneSingleton<TestConnectManager>
{
    public bool isHost = false;
    // 중앙 서버(호스트만 사용)
    public string centralServerIp = "127.0.0.1";
    public int centralServerPort = 7777;

    // 중앙 서버가 내려줬다고 가정하는 호스트 접속 정보
    public string hostIpFromServer = "127.0.0.1";
    public int hostPortFromServer = 7788;
    private void Start()
    {
        if (isHost)
        {
            // 1) 중앙 서버 연결
            // NetManager.Instance.Connect(centralServerIp, centralServerPort);

            // // 2) 연결 완료 대기 후 Host Listen 오픈
            // float timeout = 5f;
            // float elapsed = 0f;
            // while (!NetManager.Instance.IsConnected && elapsed < timeout)
            // {
            //     elapsed += Time.deltaTime;
            //     yield return null;
            // }

            // if (!NetManager.Instance.IsConnected)
            // {
            //     Debug.LogError("Host bootstrap failed: cannot connect to central server.");
            //     yield break;
            // }

            NetManager.Instance.StartHost(hostPortFromServer);
        }
        else
        {
            //NetManager.Instance.Connect("127.0.0.1", 7777);
            NetManager.Instance.Connect(hostIpFromServer, hostPortFromServer);
        }
        
    }

    public void SendLogin(string userId, string password)
    {
        PacketHandler.Instance.SendLogin(userId, password);
    }
}
