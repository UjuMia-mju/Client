using System.Collections;
using Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 선택 패널 연출 + 방장 스테이지 미리보기 동기화(C_HOST_SHOW_STAGE / S_HOST_SHOW_STAGE).
/// 참가자는 <see cref="StageManager.selectPanel"/>과 동일한 SelectPanel을 인스턴스로 띄웁니다.
/// </summary>
public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Instance { get; private set; }

    [Header("UI Pop-Up Settings")]
    [SerializeField] private float popUpDuration = 0.2f;
    [SerializeField] private Vector3 finalPanelScale = new Vector3(1f, 1f, 1f);
    
    private GameObject guestSelectPanelPrefab;

    private GameObject _currentPanel;
    private GameObject _guestHostPanel;
    private Coroutine _guestPanelFlow;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnHostShowStageEvent -= OnHostShowStagePacket;

        StopGuestPanelFlow();
        if (_guestHostPanel != null)
        {
            Destroy(_guestHostPanel);
            _guestHostPanel = null;
        }
    }

    void OnEnable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnHostShowStageEvent += OnHostShowStagePacket;
    }

    void OnDisable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnHostShowStageEvent -= OnHostShowStagePacket;

        StopGuestPanelFlow();
        if (_guestHostPanel != null)
        {
            Destroy(_guestHostPanel);
            _guestHostPanel = null;
        }
    }

    /// <summary><see cref="StageManager"/>에서 스테이지 정보 패널을 열 때 호출합니다.</summary>
    public static void NotifyHostConsideringStage(StageInfo stageInfo)
    {
        if (stageInfo == null) return;
        Instance?.TryPublishHostStage(stageInfo.MapId);
    }

    void TryPublishHostStage(int mapId)
    {
        if (mapId == 0) return;

        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
            return;

        RoomMembershipTracker.Instance?.EnsureWired();
        if (RoomMembershipTracker.Instance == null || !RoomMembershipTracker.Instance.AmIFirst())
            return;

        PacketDispatcher.Instance.SendHostShowStage(mapId);
    }

    void OnHostShowStagePacket(S_HOST_SHOW_STAGE packet)
    {
        if (packet == null) return;
        if (!IsStageSelectScene()) return;

        RoomMembershipTracker.Instance?.EnsureWired();
        if (RoomMembershipTracker.Instance != null && RoomMembershipTracker.Instance.AmIFirst())
            return;

        if (!packet.Success || packet.Stage == null)
        {
            RestartGuestSelectFlow(null);
            return;
        }

        StageInfo display = ResolveDisplayStage(packet.Stage);
        RestartGuestSelectFlow(display);
    }

    static bool IsStageSelectScene()
    {
        return SceneManager.GetActiveScene().name == Define.Scene.STAGE_SELECT;
    }

    static StageInfo ResolveDisplayStage(StageInfo fromServer)
    {
        if (fromServer == null) return null;

        if (DbCacheManager.TryGetStageInfo(fromServer.MapId, fromServer.Chapter, fromServer.Stage, out StageInfo full))
            return full;

        if (DbCacheManager.TryGetStageInfoByMapId(fromServer.MapId, out StageInfo byMap))
            return byMap;

        return fromServer;
    }

    void RestartGuestSelectFlow(StageInfo stageOrNull)
    {
        if (_guestPanelFlow != null)
        {
            StopCoroutine(_guestPanelFlow);
            _guestPanelFlow = null;
        }

        _guestPanelFlow = StartCoroutine(GuestSelectPanelFlow(stageOrNull));
    }

    void StopGuestPanelFlow()
    {
        if (_guestPanelFlow != null)
        {
            StopCoroutine(_guestPanelFlow);
            _guestPanelFlow = null;
        }
    }

    /// <summary>게스트 SelectPanel 닫기 버튼 — 플로우 중단 후 패널·미리보기 정리.</summary>
    public void CloseGuestSelectPanelFromButton()
    {
        StopGuestPanelFlow();
        StartCoroutine(CoCloseGuestSelectPanelFromButton());
    }

    IEnumerator CoCloseGuestSelectPanelFromButton()
    {
        if (_guestHostPanel != null)
        {
            yield return DynamicClosePanel(_guestHostPanel);
            Destroy(_guestHostPanel);
            _guestHostPanel = null;
        }

        StageManager sm = StageManager.Instance;
        if (sm != null)
            yield return sm.StartCoroutine(sm.CoEndGuestStagePreview());
    }

    IEnumerator GuestSelectPanelFlow(StageInfo stageOrNull)
    {
        if (_guestHostPanel != null)
        {
            yield return DynamicClosePanel(_guestHostPanel);
            Destroy(_guestHostPanel);
            _guestHostPanel = null;
        }

        var sm = StageManager.Instance;
        if (sm != null)
            yield return sm.StartCoroutine(sm.CoEndGuestStagePreview());

        if (stageOrNull == null)
        {
            _guestPanelFlow = null;
            yield break;
        }

        if (sm != null)
            yield return sm.StartCoroutine(sm.CoGuestStagePreview(stageOrNull));

        GameObject prefab = guestSelectPanelPrefab != null
            ? guestSelectPanelPrefab
            : StageManager.Instance != null
                ? StageManager.Instance.selectPanel
                : null;

        if (prefab == null)
        {
            Debug.LogWarning("[StageUIManager] SelectPanel 프리팹이 없습니다. StageManager.selectPanel 또는 guestSelectPanelPrefab을 지정하세요.");
            _guestPanelFlow = null;
            yield break;
        }

        _guestHostPanel = Instantiate(prefab);
        var ctrl = _guestHostPanel.GetComponent<SelectPanelController>();
        if (ctrl != null)
        {
            int stars = StageManager.Instance != null
                ? StageManager.Instance.GetClearStarCountForMap(stageOrNull.MapId)
                : 0;
            ctrl.SetInfo(
                stageOrNull.StageName,
                stageOrNull.Difficulty,
                stageOrNull.Description,
                stageOrNull.EstimatedClearTime,
                stars);
            ctrl.SetGuestPreviewMode(true);
        }

        RectTransform rect = _guestHostPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.zero;
        }

        Canvas canvas = _guestHostPanel.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            canvas.worldCamera = Camera.main;

        yield return DynamicPopUpPanel(_guestHostPanel);
        _guestPanelFlow = null;
    }

    public IEnumerator OpenPanel(GameObject panelPrefab, string stageName, int difficulty, string description, int estimatedClearTimeSeconds, int clearStarCount)
    {
        if (panelPrefab == null) yield break;

        _currentPanel = Instantiate(panelPrefab);

        SelectPanelController panelInfo = _currentPanel.GetComponent<SelectPanelController>();
        if (panelInfo != null)
        {
            panelInfo.SetInfo(stageName, difficulty, description, estimatedClearTimeSeconds, clearStarCount);
        }

        RectTransform rect = _currentPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.zero;
        }

        Canvas canvas = _currentPanel.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
        }

        yield return StartCoroutine(DynamicPopUpPanel(_currentPanel));
    }

    public IEnumerator ClosePanel()
    {
        if (_currentPanel != null)
        {
            yield return StartCoroutine(DynamicClosePanel(_currentPanel));
            Destroy(_currentPanel);
            _currentPanel = null;
        }
    }

    private IEnumerator DynamicPopUpPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, finalPanelScale, t);
            yield return null;
        }
        cg.alpha = 1f;
        panel.transform.localScale = finalPanelScale;
    }

    private IEnumerator DynamicClosePanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        Vector3 startScale = panel.transform.localScale;
        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            panel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
    }
}
