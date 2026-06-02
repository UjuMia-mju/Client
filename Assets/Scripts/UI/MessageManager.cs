using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <see cref="messagePrefab"/>을 매번 인스턴스하여 표시하고, 일정 시간 후 페이드아웃 뒤 <c>Destroy</c>합니다.
/// 문구는 <see cref="MessageCatalog"/> 의 stringKey 로 조회합니다.
/// </summary>
public class MessageManager : MonoBehaviorSingleton<MessageManager>
{
    [Header("메시지 카탈로그")]
    [SerializeField] private MessageCatalog catalog;

    [Header("프리팹")]
    [SerializeField] private GameObject messagePrefab;
    [Tooltip("같은 화면에 여러 메시지 시 위에 그리기")]
    [SerializeField] private int canvasSortOrder = 32000;

    [Header("표시 타이밍")]
    [SerializeField] private float visibleSecondsBeforeFade = 2f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("메시지 패널 크기 (배경 Image 기준)")]
    [SerializeField] private bool resizePanelToFitText = true;
    [SerializeField, Tooltip("텍스트 줄바꿈 최대 너비(내부). 그보다 짧으면 패널 폭이 줄어듭니다.")]
    private float messageTextMaxWidth = 900f;
    [SerializeField, Tooltip("한 줄 등 짧은 문구일 때 텍스트 영역 최소 너비.")]
    private float messageTextMinWidth = 120f;
    [SerializeField, Tooltip("텍스트 영역 최대 높이. 0이면 제한 없음.")]
    private float messageTextMaxHeight = 0f;
    [SerializeField, Range(0.3f, 1f), Tooltip("화면 너비 대비 패널 최대 비율.")]
    private float messageMaxScreenWidthRatio = 0.85f;
    [SerializeField, Tooltip("배경에 더할 여백(좌·우 합, 상·하 합).")]
    private Vector2 messagePanelExtraSize = new Vector2(56f, 40f);

    protected override void Awake()
    {
        base.Awake();
        if (catalog != null)
            MessageTexts.Initialize(catalog);
    }

    /// <summary>stringKey에 해당하는 문구를 토스트로 표시합니다.</summary>
    public void ShowKey(string key, params object[] args)
    {
        Show(MessageTexts.Format(key, args));
    }

    /// <summary>서버 ErrorMsg 유무에 따라 baseKey / withReasonKey 중 하나를 표시합니다.</summary>
    public void ShowServerError(string baseKey, string withReasonKey, string errorMsg)
    {
        if (string.IsNullOrWhiteSpace(errorMsg))
            ShowKey(baseKey);
        else
            ShowKey(withReasonKey, errorMsg.Trim());
    }

    /// <summary>정적 컨텍스트에서 토스트를 표시합니다. MessageManager가 없으면 무시합니다.</summary>
    public static void TryShowKey(string key, params object[] args)
    {
        if (Instance == null) return;
        Instance.ShowKey(key, args);
    }

    /// <summary>정적 컨텍스트에서 서버 오류 토스트를 표시합니다.</summary>
    public static void TryShowServerError(string baseKey, string withReasonKey, string errorMsg)
    {
        if (Instance == null) return;
        Instance.ShowServerError(baseKey, withReasonKey, errorMsg);
    }

    /// <summary>로그인 거부(자격 증명만 해당한다고 가정할 때).</summary>
    public void ShowLoginFailure() => ShowKey(MessageKeys.LoginInvalidCredentials);

    /// <summary>
    /// 서버가 <c>S_LOGIN.success == false</c>를 준 뒤 표시합니다.
    /// 이미 연결이 끊긴 경우에는 자격 증명 문구 대신 네트워크·통신 안내를 씁니다.
    /// </summary>
    public void ShowLoginFailureAfterServerResponse()
    {
        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
            ShowKey(MessageKeys.LoginNetworkFailure);
        else
            ShowKey(MessageKeys.LoginInvalidCredentials);
    }

    /// <summary>빈 바디·깨진 프로토buf 등 로그인 응답을 신뢰할 수 없을 때.</summary>
    public void ShowLoginResponseUnreadable() => ShowKey(MessageKeys.ProtocolUnreadable);

    /// <summary>서버 <c>S_INVITE_PLAYER.success == false</c>일 때 사유를 토스트로 표시합니다.</summary>
    public void ShowInvitePlayerFailed(string errorMsg) =>
        ShowServerError(MessageKeys.InviteFailed, MessageKeys.InviteFailedWithReason, errorMsg);

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
        {
            tmp.text = message;
            if (resizePanelToFitText)
            {
                FitMessageBackgroundToText(tmp);
                StartCoroutine(CoFitMessageBackgroundAfterLayout(tmp));
            }
        }

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

    IEnumerator CoFitMessageBackgroundAfterLayout(TMP_Text tmp)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        FitMessageBackgroundToText(tmp);
    }

    void FitMessageBackgroundToText(TMP_Text tmp)
    {
        if (tmp == null) return;

        RectTransform panelRt = FindMessagePanelRect(tmp);
        if (panelRt == null) return;

        RectTransform textRt = tmp.rectTransform;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        float maxTextWidth = messageTextMaxWidth;
        if (messageMaxScreenWidthRatio > 0f)
        {
            float screenLimit = Screen.width * messageMaxScreenWidthRatio - messagePanelExtraSize.x;
            if (screenLimit > 0f)
                maxTextWidth = Mathf.Min(maxTextWidth, screenLimit);
        }

        maxTextWidth = Mathf.Max(maxTextWidth, messageTextMinWidth);

        string text = tmp.text ?? string.Empty;
        tmp.ForceMeshUpdate();

        Vector2 unbound = tmp.GetPreferredValues(text, float.PositiveInfinity, 0);
        float innerW;
        float innerH;

        if (unbound.x <= maxTextWidth + 0.01f)
        {
            innerW = Mathf.Max(unbound.x, messageTextMinWidth);
            innerH = unbound.y;
        }
        else
        {
            Vector2 wrapped = tmp.GetPreferredValues(text, maxTextWidth, 0);
            innerW = maxTextWidth;
            innerH = wrapped.y;
        }

        if (messageTextMaxHeight > 0f && innerH > messageTextMaxHeight)
            innerH = messageTextMaxHeight;

        ApplyMessagePanelSize(panelRt, textRt, innerW, innerH);
    }

    static RectTransform FindMessagePanelRect(TMP_Text tmp)
    {
        var image = tmp.GetComponentInParent<Image>();
        if (image != null)
            return image.rectTransform;

        return tmp.transform.parent as RectTransform;
    }

    void ApplyMessagePanelSize(RectTransform panelRt, RectTransform textRt, float innerW, float innerH)
    {
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(innerW, innerH);

        panelRt.sizeDelta = new Vector2(
            innerW + messagePanelExtraSize.x,
            innerH + messagePanelExtraSize.y);
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
        ShowKey(MessageKeys.ServerDisconnected);
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
