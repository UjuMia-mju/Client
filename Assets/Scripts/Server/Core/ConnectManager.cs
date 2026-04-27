using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class ConnectManager : SceneSingleton<ConnectManager>
{
    public bool isHost = false;
    // 중앙 서버(호스트만 사용)
    public string centralServerIp = "127.0.0.1";
    public int centralServerPort = 7777;

    // 테스트용 로그인 정보 (실제에서는 UI에서 입력받거나 저장된 정보를 불러와야 함)
    private string hostId = "player1@test.com";
    private string hostPassword = "1234";
    private string peerId = "player2@test.com";
    private string peerPassword = "1234";
    public bool isSecondPeer = false; // 테스트용: 두 번째 피어로 실행할 때 true로 설정
    private string peerId2 = "player3@test.com";
    private string peerPassword2 = "1234";

    // 중앙 서버가 내려줬다고 가정하는 호스트 접속 정보
    public string hostIpFromServer = "127.0.0.1";
    public int hostPortFromServer = 7788;
    private bool _isLoginSendInProgress;

    private async void Start()
    {
        PacketSender.Instance.Init(isHost);
        await System.Threading.Tasks.Task.Delay(2000);

        // 테스트 단계에서는 Host와 Peer이 직접 연결하게 함.
        // if (isHost)
        // {
        //     HostNetManager.Instance.StartHost(hostPortFromServer);

        //     // 피어가 입장할 때마다 호스트 Enter 정보를 전송
        //     PeerPacketHandler.Instance.OnPeerEnterGameEvent += OnPeerEntered;

        //     // Player 인스턴스가 이미 생성되어 있다고 가정
        //     var player = FindFirstObjectByType<Player>();
        //     if (player != null)
        //     {
        //         player.OnNetworkReady();
        //     }
        // }
        // else
        // {
        //     //NetManager.Instance.Connect(hostIpFromServer, hostPortFromServer); -> 서버 연결하는 부분. (코드 분리는 클라분들이 해주세요)
        //     PeerNetManager.Instance.Connect(hostIpFromServer, hostPortFromServer);
        //     await System.Threading.Tasks.Task.Delay(2000); // 2초 대기 -> 테스트 단계에서 호스트 소켓에 connect된 후에 패킷을 보내야 하기 때문에 딜레이 준거임. 실제에서는 제거.

        //     // Player 인스턴스가 이미 생성되어 있다고 가정
        //     var player = FindFirstObjectByType<Player>();
        //     if (player != null)
        //     {
        //         player.OnNetworkReady();
        //     }
        // }

        // 중개 서버 연결 (일단은 그냥 중앙 서버로)
        NetManager.Instance.Connect(centralServerIp, centralServerPort);
        await System.Threading.Tasks.Task.Delay(2000); // 2초 대기 -> 테스트 단계에서 중앙 서버 소켓에 connect된 후에 패킷을 보내야 하기 때문에 딜레이 준거임. 실제에서는 제거.
        // 로그인 시도
        if (isHost)
        {
            SendLogin(hostId, hostPassword);
        }
        else if (isSecondPeer)
        {
            SendLogin(peerId2, peerPassword2);
        }
        else
        {
            SendLogin(peerId, peerPassword);
        }
        await System.Threading.Tasks.Task.Delay(3000);

        PacketDispatcher.Instance.SendEnterTestRoom();

        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnNetworkReady();
        }
    }

    /// <summary>
    /// 피어가 입장했을 때 호스트의 현재 위치를 전송
    /// </summary>
    private void OnPeerEntered(int peerId, Protocol.C_TEST_ENTER_GAME packet)
    {
        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            // 호스트는 BroadcastMove를 사용해야 함
            PacketSender.Instance.BroadcastMove(player.transform.position, player.transform.rotation);
        }
    }

    private void OnDestroy()
    {
        if (isHost)
        {
            PeerPacketHandler.Instance.OnPeerEnterGameEvent -= OnPeerEntered;
        }
    }

    public void SendLogin(string userId, string password)
    {
        if (_isLoginSendInProgress)
        {
            Debug.LogWarning("[ConnectManager] 로그인 요청 처리 중입니다.");
            return;
        }

        StartCoroutine(SendLoginWhenConnected(userId, password));
    }

    private IEnumerator SendLoginWhenConnected(string userId, string password)
    {
        _isLoginSendInProgress = true;

        if (!NetManager.Instance.IsConnected)
        {
            NetManager.Instance.Connect(centralServerIp, centralServerPort);
        }

        const float timeoutSeconds = 5f;
        float elapsed = 0f;
        while (!NetManager.Instance.IsConnected && elapsed < timeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!NetManager.Instance.IsConnected)
        {
            Debug.LogError("[ConnectManager] 로그인 서버 연결 실패(타임아웃)");
            _isLoginSendInProgress = false;
            yield break;
        }

        PacketDispatcher.Instance.SendLogin(userId, password);
        _isLoginSendInProgress = false;
    }
}