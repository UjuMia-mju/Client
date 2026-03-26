using System.Collections;
using UnityEngine;

public class ConnectManager : SceneSingleton<ConnectManager>
{
    public bool isHost = false;
    // 중앙 서버(호스트만 사용)
    public string centralServerIp = "127.0.0.1";
    public int centralServerPort = 7777;

    // 중앙 서버가 내려줬다고 가정하는 호스트 접속 정보
    public string hostIpFromServer = "127.0.0.1";
    public int hostPortFromServer = 7788;
    private async void Start()
    {
        // 테스트라 중앙 서버에 연결하는 부분
        NetManager.Instance.Connect(centralServerIp, centralServerPort);
        return;
        if (isHost)
        {
            HostNetManager.Instance.StartHost(hostPortFromServer);
        }
        else
        {
            //NetManager.Instance.Connect(hostIpFromServer, hostPortFromServer); -> 서버 연결하는 부분. (코드 분리는 클라분들이 해주세요)
            PeerNetManager.Instance.Connect(hostIpFromServer, hostPortFromServer);
            await System.Threading.Tasks.Task.Delay(2000); // 2초 대기 -> 테스트 단계에서 호스트 소켓에 connect된 후에 패킷을 보내야 하기 때문에 딜레이 준거임. 실제에서는 제거.

            // Player 인스턴스가 이미 생성되어 있다고 가정
            var player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.OnNetworkReady();
            }
        }
    }

    public void SendLogin(string userId, string password)
    {
        PacketDispatcher.Instance.SendLogin(userId, password);
    }
}
