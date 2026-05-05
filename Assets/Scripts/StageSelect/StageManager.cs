using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Protocol;
using TMPro;

[RequireComponent(typeof(StageCameraController))]
[RequireComponent(typeof(StageUIManager))]
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("UI")]
    public GameObject selectPanel; 
    [Tooltip("ESC로 열고 닫는 일시정지 UI. 루트에 Canvas가 있는 프리팹(예: StagePausePanel)")]
    [SerializeField] private GameObject stagePausePanelPrefab;
    [Tooltip("다른 멤버가 방을 나갔을 때 잠시 표시(비어 있으면 무시)")]
    [SerializeField] private TextMeshProUGUI stageRoomMemberNoticeText;

    [Header("Nodes & Environment")] 
    public List<StageNode> stageNodes = new List<StageNode>();
    public bool isMovementPaused = false;

    /// <summary>
    /// 멀티 방이면 입장 순 첫 멤버(호스트)만 행성 클릭·호버 가능. 방 미참여(멤버 목록 비어 있음)면 누구나 가능.
    /// </summary>
    public bool CanInteractWithStagePlanets()
    {
        var t = RoomMembershipTracker.Instance;
        if (t == null) return true;
        t.EnsureWired();
        if (t.OrderedIds.Count == 0) return true;
        return t.AmIFirst();
    }

    public bool IsStagePauseMenuOpen => _stagePauseInstance != null && _stagePauseInstance.activeInHierarchy;

    private StageCameraController _cameraController;
    private StageUIManager _uiManager;
    
    private StageNode _currentSelectedNode;
    private bool _isTransitioning = false;
    private bool _gameplaySceneLoadStarted;
    private GameObject[] _clickOffObjects;

    /// <summary>맵 ID별 클리어 별 개수(0~3). S_GET_CLEAR_INFO 기준.</summary>
    private readonly Dictionary<int, int> _clearStarCountByMapId = new Dictionary<int, int>();

    /// <summary>SendStartStage 시 저장해두는 대상 스테이지 정보. S_GAME_READY_TO_START 수신 시 씬 전환에 사용.</summary>
    private int _pendingMapId;
    private int _pendingChapter;
    private int _pendingStageNum;

    private Coroutine _fallbackCoroutine;

    /// <summary>스테이지 씬에서 호스트가 메인/종료로 방을 나갈 때 호출됩니다. 게스트 이동은 서버의 S_ROOM_MEMBER_LEAVE 등에 의존합니다.</summary>
    public static void NotifyHostEndingStageSessionForAllPeers()
    {
        // TODO(Server): 실제 전원 처리는 패킷 수신 후 RoomMembershipTracker.OnMemberLeave / OnLeaveRoom 경로.
        // QA: StageSelectLobbyServerContract 주석 체크 후 ServerRoomLeaveBroadcastsVerified = true.
#if UNITY_EDITOR
        if (!StageSelectLobbyServerContract.ServerRoomLeaveBroadcastsVerified)
        {
            Debug.Log(
                "[StageManager] TODO(Server) 스테이지 로비: 방장 퇴장 브로드캐스트 QA 전. " +
                "체크리스트 → StageSelectLobbyServerContract.cs");
        }
#endif
        Debug.Log(
            "[StageManager] 스테이지 선택 호스트가 방 나가기 요청. " +
            "게스트가 메인으로 가려면 서버가 퇴장/방 종료를 브로드캐스트해야 합니다(S_ROOM_MEMBER_LEAVE, S_LEAVE_ROOM 등).");

        // 서버 미구현 시 임시로 로컬만 메인으로 보내고 싶다면(멀티 테스트용)·운영에서는 절대 주석 해제 금지:
        // if (SceneManager.GetActiveScene().name == Define.Scene.STAGE_SELECT)
        //     SceneLoader.Instance.LoadScene(Define.Scene.MAIN);
    }

    GameObject _stagePauseInstance;
    bool _stagePauseMenuHoldActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cameraController = GetComponent<StageCameraController>();
        _uiManager = GetComponent<StageUIManager>();
    }

    private void Start()
    {
        GameplayReadyCoordinator.ResetForStageSelect();
        InputManager.ResetGameplaySuppressionForStageSelect();

        if (stageNodes.Count == 0)
            stageNodes = new List<StageNode>(Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None));

        foreach (var node in stageNodes)
        {
            node.Init();
        }

        _cameraController.ResetToOrigin();
        _clickOffObjects = GameObject.FindGameObjectsWithTag(Define.Tag.CLICKOFF);

        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnGetClearInfoEvent += HandleGetClearInfoResponse;
            PacketHandler.Instance.OnStartStageEvent += HandleStartStageResponse;
            PacketHandler.Instance.OnStageInfoEvent += OnStageInfoReceived;
            PacketHandler.Instance.OnGameReadyToStartEvent += HandleGameReadyToStart;
            // TODO(Server): 퇴장 표시용 — S_ROOM_MEMBER_LEAVE 가 오면 아래에서 처리(player_name 등).
            PacketHandler.Instance.OnRoomMemberLeaveEvent += OnRoomMemberLeftWhileOnStageSelect;
        }
        PacketDispatcher.Instance.SendGetClearInfo();

        if (!DbCacheManager.HasStageInfo)
        {
            Debug.Log(
                "[StageManager] StageInfo 캐시가 비어 있습니다. 로그인 직후 서버가 S_STAGE_INFO를 보내야 하며, " +
                "없으면 DB 요청을 보냅니다. (StageNode의 chapter·stage는 서버 DB의 Chapter·Stage와 같아야 합니다.)");
            DbCacheManager.RequestDbData();
            StartCoroutine(CoRetryDbDataIfCacheStillEmpty());
        }
    }

    /// <summary>씬 진입·로그인 직후 S_STAGE_INFO가 늦을 때 보조 재요청 (서버 C_GET_DB_DATA 응답 대기)</summary>
    private IEnumerator CoRetryDbDataIfCacheStillEmpty()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1.5f);
            if (DbCacheManager.HasStageInfo) yield break;
            Debug.LogWarning($"[StageManager] S_STAGE_INFO 대기 중 재요청 ({i + 1}/3) …");
            DbCacheManager.RequestDbData();
        }
    }

    private void OnDestroy()
    {
        if (PacketHandler.Instance != null) 
        {
            PacketHandler.Instance.OnGetClearInfoEvent -= HandleGetClearInfoResponse;
            PacketHandler.Instance.OnStartStageEvent -= HandleStartStageResponse;
            PacketHandler.Instance.OnGameReadyToStartEvent -= HandleGameReadyToStart;
            PacketHandler.Instance.OnStageInfoEvent -= OnStageInfoReceived;
            PacketHandler.Instance.OnRoomMemberLeaveEvent -= OnRoomMemberLeftWhileOnStageSelect;
        }

        CancelInvoke(nameof(ClearStageRoomMemberNotice));
        CloseStagePausePanel(destroyInstance: true);
    }

    private void OnStageInfoReceived(S_STAGE_INFO packet)
    {
        int n = packet?.Stages?.Count ?? 0;
        Debug.Log($"[StageManager] S_STAGE_INFO 갱신: {n}개 — 노드(Chapter,Stage)를 서버 값과 맞췄는지 확인하세요.");

        if (n > 0 && PacketDispatcher.Instance != null)
            PacketDispatcher.Instance.SendGetClearInfo();
    }

    private void HandleGetClearInfoResponse(S_GET_CLEAR_INFO packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning("[StageManager] S_GET_CLEAR_INFO 실패 — 기존 클리어 표시 유지");
            return;
        }

        _clearStarCountByMapId.Clear();
        foreach (var clearInfo in packet.StageClears)
        {
            int stars = Mathf.Clamp(clearInfo.Star, 0, 3);
            if (_clearStarCountByMapId.TryGetValue(clearInfo.MapId, out int prev))
                stars = Mathf.Max(prev, stars);
            _clearStarCountByMapId[clearInfo.MapId] = stars;
        }

        foreach (var node in stageNodes)
        {
            if (node == null) continue;

            if (DbCacheManager.TryGetStageInfoByChapterStage(node.stageLevel, node.stageIndex, out StageInfo info))
            {
                int starCount = GetClearStarCountForMap(info.MapId);
                bool isCleared = starCount > 0;
                node.isClearedStage = isCleared;
                node.SetClearState(isCleared);
            }
        }
    }

    /// <summary>서버 클리어 기록 기준 별 개수(없으면 0).</summary>
    public int GetClearStarCountForMap(int mapId)
    {
        return _clearStarCountByMapId.TryGetValue(mapId, out int n) ? n : 0;
    }

    private void Update()
    {
        foreach (var node in stageNodes)
        {
            if (node != null && node.gameObject.activeSelf)
            {
                node.UpdateScale(Time.deltaTime);
                node.UpdateMovement(Time.deltaTime, isMovementPaused);
            }
        }

        if (Keyboard.current == null || !Keyboard.current[Key.Escape].wasPressedThisFrame)
            return;

        if (_stagePauseInstance != null && _stagePauseInstance.activeInHierarchy)
        {
            CloseStagePausePanel(destroyInstance: false);
            return;
        }

        if (_currentSelectedNode != null && !_isTransitioning)
        {
            StartCoroutine(ClosePanelSequence());
            return;
        }

        OpenStagePausePanel();
    }

    void OnRoomMemberLeftWhileOnStageSelect(S_ROOM_MEMBER_LEAVE packet)
    {
        if (!IsStageSelectActiveScene()) return;
        if (packet == null || NetManager.Instance == null) return;
        if (packet.PlayerId == (ulong)NetManager.Instance._playerId) return;
        if (stageRoomMemberNoticeText == null) return;

        string name = string.IsNullOrEmpty(packet.PlayerName) ? "플레이어" : packet.PlayerName;
        stageRoomMemberNoticeText.text = $"{name}님이 방을 나갔습니다.";
        CancelInvoke(nameof(ClearStageRoomMemberNotice));
        Invoke(nameof(ClearStageRoomMemberNotice), 4f);
    }

    void ClearStageRoomMemberNotice()
    {
        if (stageRoomMemberNoticeText != null)
            stageRoomMemberNoticeText.text = string.Empty;
    }

    static bool IsStageSelectActiveScene()
    {
        return SceneManager.GetActiveScene().name == Define.Scene.STAGE_SELECT;
    }

    void OpenStagePausePanel()
    {
        if (stagePausePanelPrefab == null || _isTransitioning)
            return;

        if (_stagePauseInstance == null)
            _stagePauseInstance = Instantiate(stagePausePanelPrefab);

        _stagePauseInstance.SetActive(true);

        if (!_stagePauseMenuHoldActive)
        {
            InputManager.PushPauseMenuHold();
            _stagePauseMenuHoldActive = true;
        }
    }

    /// <param name="destroyInstance">씬 종료 시 true</param>
    void CloseStagePausePanel(bool destroyInstance)
    {
        if (_stagePauseInstance != null)
        {
            if (destroyInstance)
            {
                Destroy(_stagePauseInstance);
                _stagePauseInstance = null;
            }
            else
                _stagePauseInstance.SetActive(false);
        }

        if (_stagePauseMenuHoldActive)
        {
            InputManager.PopPauseMenuHold();
            _stagePauseMenuHoldActive = false;
        }
    }
    
    public void EnterSelectedStage()
    {
        if (_currentSelectedNode == null) return;

        int level = _currentSelectedNode.stageLevel;
        int index = _currentSelectedNode.stageIndex;

        // MapId·Chapter·Stage는 서버 DB와 한 세트. 캐시의 StageInfo 기준 (C_START_STAGE.StageIndex = StageInfo.Stage)
        if (!DbCacheManager.TryGetStageInfoByChapterStage(level, index, out StageInfo info))
            return;

        if (info.Chapter != level || info.Stage != index)
        {
            Debug.LogWarning(
                $"[StageManager] 노드({level},{index})와 StageInfo({info.Chapter},{info.Stage}) 불일치. StageInfo 기준으로 전송합니다.");
        }

        Debug.Log(
            $"[StageManager] C_START_STAGE MapId={info.MapId}, Chapter={info.Chapter}, StageIndex(=Stage)={info.Stage}");
        // S_GAME_READY_TO_START 수신 시 씬 전환에 쓸 스테이지 정보를 미리 저장
        _pendingMapId = info.MapId;
        _pendingChapter = info.Chapter;
        _pendingStageNum = info.Stage;

        Debug.Log($"[StageManager] 스테이지 시작 요청 MapId={info.MapId}, Chapter={info.Chapter}, Stage={info.Stage}");
        PacketDispatcher.Instance.SendStartStage(info.MapId, info.Chapter, info.Stage);
    }

    /// <summary>
    /// S_START_STAGE: 성공/실패 여부만 확인합니다.
    /// 씬 전환은 S_GAME_READY_TO_START에서만 수행합니다.
    /// </summary>
    private void HandleStartStageResponse(S_START_STAGE packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning("[StageManager] 서버가 스테이지 시작을 거절했습니다.");
            return;
        }

        Debug.Log("[StageManager] S_START_STAGE 성공. S_GAME_READY_TO_START 대기 중...");
        if (_fallbackCoroutine != null) StopCoroutine(_fallbackCoroutine);
        _fallbackCoroutine = StartCoroutine(FallbackIfGameReadyNotReceived());
    }

    private IEnumerator FallbackIfGameReadyNotReceived()
    {
        yield return new WaitForSeconds(3f);
        _fallbackCoroutine = null;

        if (_gameplaySceneLoadStarted) yield break;

        var tracker = RoomMembershipTracker.Instance;
        ConnectManager.Instance.SetHostRole(tracker.AmIFirst());

        GameplayReadyCoordinator.SetPendingFallback(tracker.OrderedIds, 3);
        DoLoadGameplayScene();
    }

    private void HandleGameReadyToStart(S_GAME_READY_TO_START packet)
    {
        if (_gameplaySceneLoadStarted)
            return;

        if (_fallbackCoroutine != null)
        {
            StopCoroutine(_fallbackCoroutine);
            _fallbackCoroutine = null;
        }

        bool isHost = packet.IdOrder.Count > 0
                      && packet.IdOrder[0] == (ulong)NetManager.Instance._playerId;

        Debug.Log($"[StageManager] S_GAME_READY_TO_START 수신. myId={NetManager.Instance._playerId}, " +
                  $"idOrder=[{string.Join(",", packet.IdOrder)}], isHost={isHost}");

        ConnectManager.Instance.SetHostRole(isHost);

        GameplayReadyCoordinator.SetPendingFromServer(packet);
        DoLoadGameplayScene();
    }

    /// <summary>씬 로드 진입점. 이중 호출 방지 포함.</summary>
    private void DoLoadGameplayScene()
    {
        if (_gameplaySceneLoadStarted)
            return;

        _gameplaySceneLoadStarted = true;

        int mapId = 1;
        int chapter = 1;
        int stageNum = 1;

        if (_pendingMapId != 0)
        {
            mapId = _pendingMapId;
            chapter = _pendingChapter;
            stageNum = _pendingStageNum;
            Debug.Log($"[StageManager] 스테이지 시작 허가됨! 씬 이동 준비: MapId={mapId}, Chapter={chapter}, Stage={stageNum}");
        }
        else if (_currentSelectedNode != null &&
                 DbCacheManager.TryGetStageInfoByChapterStage(
                     _currentSelectedNode.stageLevel,
                     _currentSelectedNode.stageIndex,
                     out StageInfo cached))
        {
            mapId = cached.MapId;
            chapter = cached.Chapter;
            stageNum = cached.Stage;
        }

        if (!Define.Scene.TryGetGameplayScene(mapId, chapter, stageNum, out string sceneName))
        {
            Debug.LogWarning(
                "[StageManager] 인게임 씬 매핑이 없습니다. " +
                $"map={mapId}, chapter={chapter}, stage={stageNum}. fallback={Define.Scene.GAME_1_1}");
            sceneName = Define.Scene.GAME_1_1;
        }

        Debug.Log($"[StageManager] 씬 로드: {sceneName}");

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void OnStageClicked(StageNode clickedNode)
    {
        if (_currentSelectedNode != null || _isTransitioning) return;
        _currentSelectedNode = clickedNode;

        if (!DbCacheManager.TryGetStageInfoByChapterStage(
                clickedNode.stageLevel,
                clickedNode.stageIndex,
                out StageInfo stageInfo))
        {
            if (!DbCacheManager.HasStageInfo)
            {
                stageInfo = BuildLocalFallbackStageInfo(clickedNode);
                Debug.LogWarning(
                    "[StageManager] 서버 S_STAGE_INFO가 없어 로컬 폴백으로 패널을 띄웁니다. " +
                    "StageNode의 localMapIdOverride를 실제 map_id에 맞추면 서버 스타트가 안정적입니다. " +
                    $"(임시 MapId={stageInfo.MapId})");
                DbCacheManager.MergeStageInfoEntry(stageInfo);
            }
            else
            {
                Debug.LogWarning(
                    "[StageManager] 이 노드의 (chapter, stage)가 서버 목록에 없습니다. " +
                    $"노드=({clickedNode.stageLevel},{clickedNode.stageIndex}). " +
                    $"서버에 있는 쌍: {DbCacheManager.BuildChapterStageListDebugString()} " +
                    "— 인스펙터의 stageLevel·stageIndex를 DB의 Chapter·Stage와 일치시키세요.");
                _currentSelectedNode = null;
                return;
            }
        }

        OnReceiveStageInfo(stageInfo);
    }

    private static StageInfo BuildLocalFallbackStageInfo(StageNode node)
    {
        int ch = node.stageLevel;
        int st = node.stageIndex;
        int mapId = node.localMapIdOverride != 0
            ? node.localMapIdOverride
            : ch * 100 + st;

        string name = string.IsNullOrEmpty(node.localDisplayName)
            ? $"스테이지 {ch} - {st}"
            : node.localDisplayName;

        return new StageInfo
        {
            MapId = mapId,
            Chapter = ch,
            Stage = st,
            StageName = name,
            Description = "서버 S_STAGE_INFO 미수신 — 로컬 표시(입장·시작은 mapId·서버 설정 확인).",
            Difficulty = 1,
            IsBossStage = false,
            EstimatedClearTime = 0
        };
    }

    public void OnReceiveStageInfo(StageInfo stageInfo)
    {
        if (stageInfo == null)
            return;

        StageUIManager.NotifyHostConsideringStage(stageInfo);
        StartCoroutine(OpenPanelSequence(_currentSelectedNode, stageInfo));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode, StageInfo stageInfo)
    {
        _isTransitioning = true;
        isMovementPaused = true; 

        ToggleFocusMode(targetNode, true);

        yield return StartCoroutine(_cameraController.ZoomIn(targetNode.transform));

        if (selectPanel != null)
        {
            int stars = GetClearStarCountForMap(stageInfo.MapId);
            yield return StartCoroutine(_uiManager.OpenPanel(
                selectPanel,
                stageInfo.StageName,
                stageInfo.Difficulty,
                stageInfo.Description,
                stageInfo.EstimatedClearTime,
                stars)); 
        }

        _isTransitioning = false;
    }

    private IEnumerator ClosePanelSequence()
    {
        _isTransitioning = true;

        yield return StartCoroutine(_uiManager.ClosePanel());
        yield return StartCoroutine(_cameraController.ZoomOut());

        ToggleFocusMode(null, false);

        isMovementPaused = false; 
        _currentSelectedNode = null;
        _isTransitioning = false;
    }

    private void ToggleFocusMode(StageNode targetNode, bool isFocusing)
    {
        if (_clickOffObjects != null)
        {
            foreach (var obj in _clickOffObjects)
            {
                if (obj != null) obj.SetActive(!isFocusing);
            }
        }

        foreach (var node in stageNodes)
        {
            if (node == null) continue;

            if (isFocusing)
            {
                if (node == targetNode) node.gameObject.SetActive(true);
                else node.gameObject.SetActive(false);
            }
            else
            {
                node.gameObject.SetActive(true);
            }
        }
    }
}
