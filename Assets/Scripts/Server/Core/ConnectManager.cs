using System.Collections;
using System.Threading.Tasks;
using Protocol;
using UnityEngine;

public class ConnectManager : MonoBehaviour
{
    public static ConnectManager Instance { get; private set; }
    public bool isHost = false;

    public string centralServerIp = "127.0.0.1";
    public int centralServerPort = 7777;
    public string hostIpFromServer = "127.0.0.1";
    public int hostPortFromServer = 7788;

    private bool _isLoginSendInProgress;

    private async void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PacketSender.Instance.Init(isHost);
        await Task.Delay(2000);
        NetManager.Instance.Connect(centralServerIp, centralServerPort);
    }

    /// <summary>
    /// S_GAME_READY_TO_START 또는 S_START_STAGE 성공 후 호스트/피어 역할을 확정합니다.
    /// </summary>
    public void SetHostRole(bool host)
    {
        isHost = host;
        PacketSender.Instance.Init(isHost);
        Debug.Log($"[ConnectManager] 호스트 역할 확정: isHost={isHost}");
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
            NetManager.Instance.Connect(centralServerIp, centralServerPort);

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