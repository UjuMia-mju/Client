using TMPro;
using UnityEngine;

/// <summary>
/// 로그인 등 네트워크 연결 상태를 <see cref="networkingText"/>에 표시합니다.
/// </summary>
public class NetworkingUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text networkingText;

    static readonly Color ColorBlack = Color.white;
    static readonly Color ColorAccent = new Color(0f, 167f / 255f, 1f, 1f);

    const string TextConnecting = "서버 연결 중...";
    const string TextConnected = "서버 연결 완료!";
    const string TextReconnectingHint = "\n<size=85%>(자동 재시도 중)</size>";

    [SerializeField, Tooltip("이 시간(초) 넘게 미연결이면 '자동 재시도' 안내를 덧붙입니다.")]
    private float showReconnectHintAfterSeconds = 3f;

    [SerializeField, Tooltip("초당 깜빡임 주기에 비례 (값이 클수록 빠름)")]
    private float pulseSpeed = 1.15f;

    bool _isPulsing;
    bool _isConnected;
    float _connectingSinceRealtime;

    void Awake()
    {
        if (networkingText == null)
            networkingText = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        var nm = NetManager.Instance;
        if (nm != null)
        {
            nm.OnConnectedToServer += OnServerTcpConnected;
            nm.OnDisconnected += OnServerDisconnected;
        }

        EnterConnectingState();
        if (nm != null && nm.IsConnected)
            OnServerTcpConnected();
    }

    void OnDisable()
    {
        var nm = NetManager.Instance;
        if (nm != null)
        {
            nm.OnConnectedToServer -= OnServerTcpConnected;
            nm.OnDisconnected -= OnServerDisconnected;
        }
    }

    void EnterConnectingState()
    {
        _isConnected = false;
        _isPulsing = true;
        _connectingSinceRealtime = Time.unscaledTime;
        if (networkingText == null) return;

        networkingText.text = TextConnecting;
        networkingText.color = ColorBlack;
    }

    void OnServerTcpConnected()
    {
        _isConnected = true;
        _isPulsing = false;
        if (networkingText == null) return;

        networkingText.text = TextConnected;
        networkingText.color = ColorAccent;
    }

    void OnServerDisconnected()
    {
        EnterConnectingState();
    }

    void Update()
    {
        if (!_isPulsing || networkingText == null || _isConnected)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        networkingText.color = Color.Lerp(ColorBlack, ColorAccent, t);

        if (Time.unscaledTime - _connectingSinceRealtime < showReconnectHintAfterSeconds)
            return;

        string desired = TextConnecting + TextReconnectingHint;
        if (networkingText.text != desired)
            networkingText.text = desired;
    }
}
