using System.Collections;
using System.Threading.Tasks;
using Protocol;
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
    private bool _isLoginSendInProgress;

    private async void Start()
    {
        PacketSender.Instance.Init(isHost);
        await Task.Delay(2000);

        // 중개 서버 연결
        NetManager.Instance.Connect(centralServerIp, centralServerPort);
        // 로그인은 LoginManager UI를 통해 수동으로 진행
    }

    /// <summary>
    /// S_GAME_READY_TO_START 수신 후 서버가 내려준 id_order[0] 기준으로
    /// 호스트/피어 역할을 확정하고 PacketSender를 재초기화합니다.
    /// </summary>
    public void SetHostRole(bool host)
    {
        isHost = host;
        PacketSender.Instance.Init(isHost);
        Debug.Log($"[ConnectManager] 호스트 역할 확정: isHost={isHost}");
    }

    private void OnDestroy()
    {
        if (isHost)
        {
            PeerPacketHandler.Instance.OnPeerEnterGameEvent -= OnPeerEntered;
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
            PacketSender.Instance.BroadcastMove(player.transform.position, player.transform.rotation);
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