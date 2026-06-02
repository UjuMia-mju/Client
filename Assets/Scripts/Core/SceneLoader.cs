using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Scene 변경과 변경 시 Fade 연출 (UIPanelAnimator 활용).
/// Fade·Loading 패널은 인스펙터에 프리팹만 할당하고, 런타임에 DontDestroy로 생성합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    [Header("페이드 설정")]
    [SerializeField] private GameObject fadePrefab;

    [Header("로딩 UI")]
    [SerializeField] private GameObject loadingPanelPrefab;
    [SerializeField]
    [Tooltip("로딩이 금방 끝나도 패널이 최소 이 시간(Realtime 초) 동안 보이게 합니다.")]
    private float minLoadingPanelRealtimeSeconds = 1.2f;

    /// <summary>FadePrefab(SortingOrder 보통 10)보다 위에 두어 검은 페이드 위에 진행 표시가 보이도록 합니다.</summary>
    private const int LoadingCanvasSortOrder = 15;

    private GameObject fadeInstance;
    private CanvasGroup fadeCanvasGroup;

    // Fade 프리팹에 포함되거나 런타임에 부착된 UIPanelAnimator
    private UIPanelAnimator panelAnimator;

    private GameObject loadingInstance;
    private LoadingUIController loadingUi;

    protected override void Awake()
    {
        base.Awake();
        InitFadeCanvas();
        InitLoadingPanel();
    }

    /// <summary>Splash 로고 연출 후 Login으로 가는 최초 전환만 로딩 패널을 쓰지 않습니다.</summary>
    static bool IsSplashToLoginTransition(string destinationSceneName)
    {
        var s = SceneManager.GetActiveScene();
        if (!s.IsValid())
            return false;
        return s.name == Define.Scene.SPLASH && destinationSceneName == Define.Scene.LOGIN;
    }

    private void InitFadeCanvas()
    {
        if (fadeInstance == null && fadePrefab != null)
        {
            UIPanelAnimator.ClearStandaloneSingletonHostBeforeFadeInstall();

            fadeInstance = Instantiate(fadePrefab);
            fadeInstance.transform.SetParent(null);
            DontDestroyOnLoad(fadeInstance);

            fadeCanvasGroup = fadeInstance.GetComponent<CanvasGroup>();

            fadeInstance.SetActive(false);
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }

            panelAnimator = fadeInstance.GetComponent<UIPanelAnimator>();
            if (panelAnimator == null)
                panelAnimator = fadeInstance.AddComponent<UIPanelAnimator>();
        }
    }

    private void InitLoadingPanel()
    {
        if (loadingInstance != null || loadingPanelPrefab == null)
            return;

        loadingInstance = Instantiate(loadingPanelPrefab);
        loadingInstance.transform.SetParent(null);
        DontDestroyOnLoad(loadingInstance);

        loadingUi = loadingInstance.GetComponent<LoadingUIController>();
        if (loadingUi == null)
        {
            Debug.LogWarning($"{nameof(SceneLoader)}: {loadingPanelPrefab.name}에 {nameof(LoadingUIController)}가 없습니다.", this);
            Destroy(loadingInstance);
            loadingInstance = null;
            return;
        }

        var canvas = loadingInstance.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = LoadingCanvasSortOrder;
        }

        loadingInstance.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        if (fadeInstance == null) InitFadeCanvas();
        if (loadingInstance == null) InitLoadingPanel();

        bool useLoadingUi = !IsSplashToLoginTransition(sceneName);

        StopAllCoroutines();
        if (CanRunFadeLoad())
        {
            StartCoroutine(LoadAsyncSequenceWithFade(sceneName, useLoadingUi));
        }
        else
        {
            StartCoroutine(LoadAsyncSequenceNoFade(sceneName, useLoadingUi));
        }
    }

    private bool CanRunFadeLoad()
    {
        return fadeInstance != null && fadeCanvasGroup != null && panelAnimator != null;
    }

    /// <summary>fadePrefab 미배정 등 — 페이드 없이 비동기 씬 전환만 수행 (모든 경로를 SceneLoader로 맞출 때용)</summary>
    private IEnumerator LoadAsyncSequenceNoFade(string sceneName, bool useLoadingUi)
    {
        float panelShownRealtime = 0f;

        if (useLoadingUi)
        {
            ShowLoadingPanel();
            panelShownRealtime = Time.realtimeSinceStartup;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            if (useLoadingUi && loadingUi != null)
                loadingUi.ApplyAsyncOperationProgress(op);

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        if (useLoadingUi)
        {
            if (loadingUi != null)
                loadingUi.SnapToCompletedDisplay();
            yield return EnforceMinimumLoadingPanelVisible(panelShownRealtime);
            HideLoadingPanel();
        }
    }

    private IEnumerator LoadAsyncSequenceWithFade(string sceneName, bool useLoadingUi)
    {
        fadeInstance.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(panelAnimator.FadeIn(fadeInstance, Vector3.one));

        yield return null;

        float panelShownRealtime = 0f;
        if (useLoadingUi)
        {
            ShowLoadingPanel();
            panelShownRealtime = Time.realtimeSinceStartup;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            if (useLoadingUi && loadingUi != null)
                loadingUi.ApplyAsyncOperationProgress(op);

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        if (useLoadingUi)
        {
            if (loadingUi != null)
                loadingUi.SnapToCompletedDisplay();
            yield return EnforceMinimumLoadingPanelVisible(panelShownRealtime);
            HideLoadingPanel();
        }

        yield return new WaitForSecondsRealtime(0.2f);

        yield return StartCoroutine(panelAnimator.FadeOut(fadeInstance, destroyOnEnd: false));

        fadeCanvasGroup.blocksRaycasts = false;
        fadeInstance.SetActive(false);
    }

    private IEnumerator EnforceMinimumLoadingPanelVisible(float panelShownRealtime)
    {
        if (loadingUi == null || minLoadingPanelRealtimeSeconds <= 0f)
            yield break;

        float elapsed = Time.realtimeSinceStartup - panelShownRealtime;
        float shortfall = minLoadingPanelRealtimeSeconds - elapsed;
        if (shortfall > 0f)
            yield return new WaitForSecondsRealtime(shortfall);
    }

    void ShowLoadingPanel()
    {
        ShowLoadingOverlay(resetProgress: !IsLoadingOverlayVisible);
    }

    void HideLoadingPanel()
    {
        if (loadingUi == null)
            return;
        loadingUi.Hide();
    }

    /// <summary>씬 전환 없이 로딩 패널만 표시(스테이지 시작 대기·싱글 부트스트랩 등).</summary>
    public void ShowLoadingOverlay(bool resetProgress = true)
    {
        if (loadingInstance == null)
            InitLoadingPanel();
        if (loadingUi == null)
            return;
        if (resetProgress || !IsLoadingOverlayVisible)
            loadingUi.ShowAndResetProgress();
        else
            loadingInstance.SetActive(true);
    }

    /// <summary><see cref="ShowLoadingOverlay"/>로 켠 패널을 숨깁니다.</summary>
    public void HideLoadingOverlay()
    {
        HideLoadingPanel();
    }

    public bool IsLoadingOverlayVisible =>
        loadingInstance != null && loadingInstance.activeSelf;
}
