using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <see cref="messagePrefab"/>을 매번 인스턴스하여 표시하고, 일정 시간 후 페이드아웃 뒤 <c>Destroy</c>합니다.
/// 프리팹 루트(또는 자식)에 <see cref="Canvas"/>가 있어야 하며, 본문은 <see cref="TMP_Text"/>로 찾습니다.
/// </summary>
public class MessageManager : MonoBehaviorSingleton<MessageManager>
{
    [Header("프리팹")]
    [SerializeField] private GameObject messagePrefab;
    [Tooltip("같은 화면에 여러 메시지 시 위에 그리기")]
    [SerializeField] private int canvasSortOrder = 32000;

    const string LoginFailureMessage = "아이디 또는 비밀번호가 올바르지 않습니다.";
    const string LoginNetworkOrProtocolMessage =
        "서버와의 연결을 실패했습니다.";
    const string ServerDisconnectedMessage = "서버와의 연결이 끊어졌습니다.";

    [Header("표시 타이밍")]
    [SerializeField] private float visibleSecondsBeforeFade = 2f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    /// <summary>로그인 거부(자격 증명만 해당한다고 가정할 때).</summary>
    public void ShowLoginFailure() => Show(LoginFailureMessage);

    /// <summary>
    /// 서버가 <c>S_LOGIN.success == false</c>를 준 뒤 표시합니다.
    /// 이미 연결이 끊긴 경우에는 자격 증명 문구 대신 네트워크·통신 안내를 씁니다.
    /// </summary>
    public void ShowLoginFailureAfterServerResponse()
    {
        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
            Show(LoginNetworkOrProtocolMessage);
        else
            Show(LoginFailureMessage);
    }

    /// <summary>빈 바디·깨진 프로토buf 등 로그인 응답을 신뢰할 수 없을 때.</summary>
    public void ShowLoginResponseUnreadable() => Show(LoginNetworkOrProtocolMessage);

    /// <summary>기본 대기·페이드 시간으로 표시합니다.</summary>
    public void Show(string message) => Show(message, visibleSecondsBeforeFade, fadeOutDuration);

    /// <param name="message">본문</param>
    /// <param name="visibleSeconds">페이드 시작 전 표시 시간(실시간 초)</param>
    /// <param name="fadeSeconds">페이드아웃 길이(실시간 초)</param>
    public void Show(string message, float visibleSeconds, float fadeSeconds)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (messagePrefab == null)
        {
            Debug.LogWarning("[MessageManager] messagePrefab이 비어 있습니다.");
            return;
        }

        var instance = Instantiate(messagePrefab, transform);
        instance.SetActive(true);

        var canvas = instance.GetComponent<Canvas>();
        if (canvas == null)
            canvas = instance.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            canvas.sortingOrder = canvasSortOrder;

        var cg = instance.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = instance.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        var tmp = instance.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = message;

        bool skipWait = false;
        var btn = instance.GetComponentInChildren<Button>(true);
        if (btn != null)
            btn.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySFX("Click2");
                skipWait = true;
            });

        StartCoroutine(CoLifecycle(instance, cg, visibleSeconds, fadeSeconds, () => skipWait));
    }

    void OnEnable()
    {
        NetManager.Instance.OnDisconnected += OnServerDisconnected;
    }

    void OnDisable()
    {
        if (NetManager.Instance != null)
            NetManager.Instance.OnDisconnected -= OnServerDisconnected;
    }

    void OnServerDisconnected()
    {
        Show(ServerDisconnectedMessage);
    }

    IEnumerator CoLifecycle(GameObject instance, CanvasGroup cg, float visibleSeconds, float fadeSeconds, System.Func<bool> shouldSkipWait)
    {
        float elapsed = 0f;
        while (elapsed < visibleSeconds && !(shouldSkipWait != null && shouldSkipWait()))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fadeSeconds <= 0f)
        {
            Destroy(instance);
            yield break;
        }

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }

        Destroy(instance);
    }
}
