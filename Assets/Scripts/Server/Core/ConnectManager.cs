using System.Collections;
using Protocol;
using UnityEngine;

public class ConnectManager : MonoBehaviour
{
    public static ConnectManager Instance { get; private set; }
    public bool isHost = false;

    public string centralServerIp = "54.116.132.68";
    public int centralServerPort = 7777;
    public string hostIpFromServer = "127.0.0.1";
    public int hostPortFromServer = 7788;

    [Header("Reconnect")]
    [SerializeField, Tooltip("첫 연결 시도 전 대기(초). 기존 Start 지연과 동일.")]
    private float initialConnectDelaySeconds = 2f;
    [SerializeField, Tooltip("연결 안 됐을 때 재시도 간격(실시간 초, Time.timeScale 무관).")]
    private float reconnectIntervalSeconds = 2.5f;

    private bool _isLoginSendInProgress;
    private Coroutine _connectionRoutine;

    private void Start()
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
        _connectionRoutine = StartCoroutine(PersistTcpConnectionLoop());
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        if (_connectionRoutine != null)
        {
            StopCoroutine(_connectionRoutine);
            _connectionRoutine = null;
        }

        Instance = null;
    }

    /// <summary>서버에 붙을 때까지 주기적으로 Connect를 시도합니다(이미 시도 중이면 건너뜀).</summary>
    IEnumerator PersistTcpConnectionLoop()
    {
        var initialWait = new WaitForSecondsRealtime(initialConnectDelaySeconds);
        var retryWait = new WaitForSecondsRealtime(reconnectIntervalSeconds);

        yield return initialWait;

        while (true)
        {
            var nm = NetManager.Instance;
            if (nm != null && !nm.IsConnected && !nm.HasPendingConnect)
                nm.Connect(centralServerIp, centralServerPort);

            yield return retryWait;
        }
    }

    /// <summary>
    /// S_GAME_READY_TO_START 또는 S_START_STAGE 성공 후 호스트/피어 역할을 확정합니다.
    /// </summary>
    public void SetHostRole(bool host)
    {
        Debug.Log($"[ConnectManager] SetHostRole. before={isHost}, after={host}\n{System.Environment.StackTrace}");
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

        const float timeoutSeconds = 20f;
        float elapsed = 0f;
        while (!NetManager.Instance.IsConnected && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!NetManager.Instance.IsConnected)
        {
            Debug.LogError("[ConnectManager] 로그인 서버 연결 실패(타임아웃)");
            MessageManager.Instance.Show("서버에 연결할 수 없습니다. 네트워크와 주소를 확인해 주세요.");
            _isLoginSendInProgress = false;
            yield break;
        }

        PacketDispatcher.Instance.SendLogin(userId, password);
        _isLoginSendInProgress = false;
    }
}