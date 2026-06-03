using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas 프리팹 루트에 붙입니다. Id_order 순으로 프로필을 채우고 남은 시작 초를 표시합니다.
/// Profile(Image)와 NameText(TMP, Profile의 직속 자식이 아니면 인스펙터에서만 연결)는 위치 변경 없이 레퍼런스와 텍스트만 사용합니다.
/// </summary>
public class ReadyToStartPanelController : MonoBehaviour
{
    [Serializable]
    public sealed class ReadyProfileSlot
    {
        public GameObject Root;
        public Image Profile;
        public TMP_Text NameText;
    }

    [Header("Countdown")]
    [SerializeField] private TMP_Text readySecondText;

    private string countdownFormat = "{0}초 뒤 시작됩니다...";

    [Header("Profiles")]
    [SerializeField] private List<ReadyProfileSlot> profileSlots = new();

    [Header("표시 옵션")]
    [SerializeField] private Color readyTint = Color.white;
    [SerializeField] private Color notReadyTint = new(0.7f, 0.7f, 0.7f, 1f);
    [Tooltip("방장(IdOrder[0]) 행의 Profile 이미지 스프라이트. 비어 있으면 기본 스프라이트 유지.")]
    [SerializeField] private Sprite hostProfileSprite;

    [Header("등장 연출")]
    [SerializeField] private float fadeInDuration = 0.38f;
    [SerializeField] private float fadeOutDuration = 0.22f;

    private Coroutine _countdownCoroutine;
    private readonly List<ulong> _activeOrderIds = new();
    private Action _onCountdownEnded;
    private bool _presentationActive;

    private Tween _panelFadeTween;

    /// <summary>이 인스턴스가 <see cref="InputManager"/> Ready 홀드를 걸었는지.</summary>
    private bool _holdsGameplayInput;

    /// <summary>Profile 이미지별 비(非)방장 스프라이트(최초 레퍼런스 캐시).</summary>
    readonly Dictionary<Image, Sprite> _baselineProfileSpriteByImage = new();

    /// <summary>인스펙터·하이어라키 기준 프로필/텍스트 바인딩을 수행합니다. Begin 전 재호출해도 안전합니다.</summary>
    public void WarmUpBindings()
    {
        BuildSlotsIfNeeded();
        BindAllProfiles();
        ResolveReadySecondGlobal();
        CacheBaselineProfileSprites();
    }

    private void Awake()
    {
        WarmUpBindings();
        PrepareCanvasAlphaForInactive();
    }

    /// <summary>스테이지 선택에서 플레이 직후: 서버 S_GAME_READY_TO_START 전까지 대기용으로 패널만 켭니다. 카운트다운은 게임 씬에서 소비됩니다.</summary>
    public void ActivateForPlayRequestStaging()
    {
        HideAllProfiles();
        if (readySecondText != null)
            readySecondText.text = string.Empty;
        WarmUpBindings();

        KillPanelFadeTween();
        gameObject.SetActive(true);

        ShowOrRefreshPanel(animateFadeIn: fadeInDuration > 0f);
        AcquireGameplayInputHold();
    }

    /// <summary>S_GAME_READY_TO_START 기준 남은 시간 표시. 카운트다운이 끝나면 onFinished 호출(스테이지 선택에서는 씬 전환, 인게임에서는 PlayManager가 게이트 해제).</summary>
    public void BeginFromPacket(S_GAME_READY_TO_START packet, Action onFinished)
    {
        if (packet == null)
        {
            onFinished?.Invoke();
            return;
        }

        float delaySec = Mathf.Max(0f, packet.StartDelaySeconds);
        _ = packet.RandomSeed;

        if (packet.ServerStartTimestamp > 0)
        {
            double serverUnix = packet.ServerStartTimestamp > 999_999_999_999uL
                ? packet.ServerStartTimestamp / 1000.0
                : packet.ServerStartTimestamp;
            double nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double remain = serverUnix - nowUtc;
            if (remain > 0 && remain <= delaySec + 120)
                delaySec = (float)remain;
        }

        Begin(packet.IdOrder, delaySec, onFinished);
    }

    /// <summary>
    /// S_GAME_READY_TO_START 폴백: id 순서만으로 표시 후 delay 초 뒤 onFinished.
    /// </summary>
    public void BeginFallback(IReadOnlyList<ulong> idOrder, int delaySeconds, Action onFinished)
    {
        Begin(idOrder ?? Array.Empty<ulong>(), Mathf.Max(0f, delaySeconds), onFinished);
    }

    /// <summary>씬 이탈·세션 정리 시 남아 있는 패널·홀드·트윈을 모두 끕니다.</summary>
    public static void DismissAllActive()
    {
        var panels = UnityEngine.Object.FindObjectsByType<ReadyToStartPanelController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            panel.AbortCountdown();
            panel.KillPanelFadeTween();
            panel.gameObject.SetActive(false);
        }
    }

    public void AbortCountdown(Action stillInvokeCallback = null)
    {
        StopCountdownCoroutine();
        UnhookMemberCacheChanged();
        KillPanelFadeTween();

        HideAllProfiles();
        if (readySecondText != null)
            readySecondText.text = string.Empty;

        void FinishAbort()
        {
            stillInvokeCallback?.Invoke();
            _onCountdownEnded = null;
            _activeOrderIds.Clear();
            _presentationActive = false;
        }

        HidePanelAnimated(FinishAbort);
    }

    private void Begin(IReadOnlyList<ulong> idOrder, float delaySeconds, Action onFinished)
    {
        RoomMemberDisplayCache.Instance.WarmUp();
        WarmUpBindings();
        StopCountdownCoroutine();

        UnhookMemberCacheChanged();
        _presentationActive = false;

        _onCountdownEnded = onFinished;
        HideAllProfiles();

        if (!gameObject.activeSelf || CanvasGroupAlpha < 0.99f)
            ShowOrRefreshPanel(animateFadeIn: fadeInDuration > 0f);
        else
            FinishRevealImmediate();

        _activeOrderIds.Clear();
        if (idOrder != null && idOrder.Count > 0)
            _activeOrderIds.AddRange(idOrder);
        else if (NetManager.Instance != null)
            _activeOrderIds.Add(NetManager.Instance._playerId);

        ulong hostHint = _activeOrderIds.Count > 0 ? _activeOrderIds[0] : 0;
        FlushProfileRows(hostHint);

        RestartCountdownRealtime(Mathf.Max(0f, delaySeconds), InvokeFinished);

        _presentationActive = true;
        HookMemberCacheChanged();
        AcquireGameplayInputHold();
    }

    private void HookMemberCacheChanged()
    {
        RoomMemberDisplayCache.Instance.Changed -= OnMemberDisplayCacheChanged;
        RoomMemberDisplayCache.Instance.Changed += OnMemberDisplayCacheChanged;
    }

    private void UnhookMemberCacheChanged()
    {
        RoomMemberDisplayCache.Instance.Changed -= OnMemberDisplayCacheChanged;
    }

    private void OnMemberDisplayCacheChanged()
    {
        if (!_presentationActive || _activeOrderIds.Count <= 0)
            return;

        ulong hostHint = _activeOrderIds[0];
        FlushProfileRows(hostHint);
    }

    private void InvokeFinished()
    {
        StopCountdownCoroutine();
        UnhookMemberCacheChanged();
        _presentationActive = false;

        var cb = _onCountdownEnded;
        _onCountdownEnded = null;
        _activeOrderIds.Clear();

        // 패널은 StageSelect 언로드로 사라질 때까지 페이드아웃·비활성화하지 않음
        cb?.Invoke();
    }

    private void RestartCountdownRealtime(float durationSeconds, Action callback)
    {
        StopCountdownCoroutine();
        _countdownCoroutine = StartCoroutine(
            CoRealtimeCountdown(Time.realtimeSinceStartup + Mathf.Max(0f, durationSeconds), callback));
    }

    private IEnumerator CoRealtimeCountdown(float endRealtimeSinceStartup, Action onDone)
    {
        while (Time.realtimeSinceStartup < endRealtimeSinceStartup - 1e-3f)
        {
            float rem = endRealtimeSinceStartup - Time.realtimeSinceStartup;
            UpdateSecondsLabel(Mathf.CeilToInt(Mathf.Max(0f, rem)));
            yield return null;
        }

        UpdateSecondsLabel(0);
        onDone?.Invoke();
        _countdownCoroutine = null;
    }

    private void UpdateSecondsLabel(int secondsRoundedUp)
    {
        if (readySecondText != null && !string.IsNullOrEmpty(countdownFormat))
            readySecondText.text = string.Format(countdownFormat, secondsRoundedUp);
    }

    private void FlushProfileRows(ulong hostId)
    {
        for (int i = 0; i < profileSlots.Count; i++)
        {
            var slot = profileSlots[i];
            if (!IsSlotConfigured(slot))
            {
                if (slot.Root != null)
                    slot.Root.SetActive(false);
                continue;
            }

            if (i >= _activeOrderIds.Count)
            {
                slot.Root?.SetActive(false);
                continue;
            }

            ulong pid = _activeOrderIds[i];
            string nameLine = ResolvePlayerLabel(pid);

            if (slot.Profile != null)
            {
                ApplyHostProfileSprite(slot.Profile, pid == hostId);
                slot.Profile.color = IsMemberReadyForDisplay(pid) ? readyTint : notReadyTint;
            }

            if (slot.NameText != null)
            {
                var lines = new List<string> { nameLine };
                if (IsMemberReadyForDisplay(pid))
                    lines.Add("준비");
                slot.NameText.text = string.Join("\n", lines);
            }
            slot.Root?.SetActive(true);
        }
    }

    void CacheBaselineProfileSprites()
    {
        foreach (ReadyProfileSlot slot in profileSlots)
        {
            if (slot?.Profile == null)
                continue;
            if (_baselineProfileSpriteByImage.ContainsKey(slot.Profile))
                continue;
            _baselineProfileSpriteByImage[slot.Profile] = slot.Profile.sprite;
        }
    }

    void ApplyHostProfileSprite(Image img, bool isHost)
    {
        if (img == null)
            return;
        if (!_baselineProfileSpriteByImage.TryGetValue(img, out Sprite baseline))
        {
            baseline = img.sprite;
            _baselineProfileSpriteByImage[img] = baseline;
        }

        if (isHost && hostProfileSprite != null)
            img.sprite = hostProfileSprite;
        else
            img.sprite = baseline;
    }

    static bool IsMemberReadyForDisplay(ulong playerId)
    {
        if (!RoomMemberDisplayCache.IsLobbyReadyDisplayRelevant)
            return false;

        return RoomMemberDisplayCache.Instance.TryGet(playerId, out var e) && e.IsReady;
    }

    static string ResolvePlayerLabel(ulong playerId)
    {
        if (RoomMemberDisplayCache.Instance.TryGet(playerId, out var e) && !string.IsNullOrEmpty(e.DisplayName))
            return e.DisplayName;

        var nm = NetManager.Instance;
        if (nm != null && playerId == nm._playerId && !string.IsNullOrEmpty(nm.PlayerName))
            return nm.PlayerName;

        return $"Player {playerId}";
    }

    private void HideAllProfiles()
    {
        foreach (ReadyProfileSlot s in profileSlots)
        {
            if (s.Root != null)
                s.Root.SetActive(false);
        }
    }

    private void HidePanelAnimated(Action onFullyHidden)
    {
        KillPanelFadeTween();

        if (!gameObject.activeSelf)
        {
            PrepareCanvasAlphaForInactive();
            onFullyHidden?.Invoke();
            return;
        }

        var cg = CanvasGroupEnsure();
        if (fadeOutDuration <= 0f)
        {
            cg.alpha = 0f;
            gameObject.SetActive(false);
            PrepareCanvasAlphaForInactive();
            onFullyHidden?.Invoke();
            return;
        }

        cg.interactable = false;
        cg.blocksRaycasts = false;
        _panelFadeTween = cg
            .DOFade(0f, fadeOutDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                PrepareCanvasAlphaForInactive();
                onFullyHidden?.Invoke();
            });
    }

    /// <summary>비활성 상태에서 다음 활성 시 페이드를 위해 알파 초기값을 맞춥니다.</summary>
    void PrepareCanvasAlphaForInactive()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0f;
    }

    CanvasGroup CanvasGroupEnsure()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    float CanvasGroupAlpha
    {
        get
        {
            var cg = GetComponent<CanvasGroup>();
            return cg != null ? cg.alpha : 1f;
        }
    }

    void KillPanelFadeTween()
    {
        _panelFadeTween?.Kill();
        _panelFadeTween = null;
    }

    void ShowOrRefreshPanel(bool animateFadeIn)
    {
        gameObject.SetActive(true);
        KillPanelFadeTween();

        var cg = CanvasGroupEnsure();

        if (!animateFadeIn || fadeInDuration <= 0f)
        {
            cg.alpha = 1f;
            ApplyInteractableRevealState();
            return;
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        _panelFadeTween = cg
            .DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => ApplyInteractableRevealState());
    }

    void FinishRevealImmediate()
    {
        gameObject.SetActive(true);
        KillPanelFadeTween();
        var cg = CanvasGroupEnsure();
        cg.alpha = 1f;
        ApplyInteractableRevealState();
    }

    void ApplyInteractableRevealState()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) return;

        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void StopCountdownCoroutine()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    private void OnDisable()
    {
        ReleaseGameplayInputHold();
    }

    private void OnDestroy()
    {
        ReleaseGameplayInputHold();
        KillPanelFadeTween();
        StopCountdownCoroutine();
        UnhookMemberCacheChanged();
    }

    void AcquireGameplayInputHold()
    {
        if (_holdsGameplayInput)
            return;
        _holdsGameplayInput = true;
        InputManager.PushReadyPanelHold();
    }

    void ReleaseGameplayInputHold()
    {
        if (!_holdsGameplayInput)
            return;
        _holdsGameplayInput = false;
        InputManager.PopReadyPanelHold();
    }

    private void BuildSlotsIfNeeded()
    {
        if (profileSlots.Count > 0)
            return;

        Transform container = transform.Find("Profiles");
        if (container == null)
            container = DeepFind(transform, "Profiles");
        if (container == null)
            return;

        profileSlots.Clear();
        for (int c = 0; c < container.childCount; c++)
        {
            var child = container.GetChild(c).gameObject;
            profileSlots.Add(new ReadyProfileSlot { Root = child });
        }

        foreach (ReadyProfileSlot s in profileSlots)
            BindProfileSlotBindings(s);

        foreach (ReadyProfileSlot s in profileSlots)
            s.Root?.SetActive(false);
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
            return null;
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t;
        }

        return null;
    }

    private void BindAllProfiles()
    {
        foreach (ReadyProfileSlot s in profileSlots)
            BindProfileSlotBindings(s);

        ResolveReadySecondGlobal();
    }

    /// <summary>
    /// 미할당 필드만: Root 직속 "Profile"(Image), Profile 직속 "NameText"(TMP)로 연결합니다.
    /// 이름/위치 세팅은 인스펙터 연결 또는 위 고정 계층을 사용합니다(DeepFind로 다른 오브젝트를 묶지 않음).
    /// </summary>
    static void BindProfileSlotBindings(ReadyProfileSlot s)
    {
        if (s == null || s.Root == null)
            return;

        if (s.Profile == null)
        {
            var profileTr = s.Root.transform.Find("Profile");
            if (profileTr != null)
                s.Profile = profileTr.GetComponent<Image>();
        }

        if (s.NameText == null && s.Profile != null)
        {
            var nameTr = s.Profile.transform.Find("NameText");
            if (nameTr != null)
                s.NameText = nameTr.GetComponent<TMP_Text>();
        }
    }
    private void ResolveReadySecondGlobal()
    {
        if (readySecondText != null)
            return;

        Transform t = DeepFind(transform, "ReadySecondText");
        if (t != null)
            readySecondText = t.GetComponent<TMP_Text>();
    }

    private static bool IsSlotConfigured(ReadyProfileSlot slot)
        => slot != null && slot.Root != null;
}
