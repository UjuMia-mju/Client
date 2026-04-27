using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Protocol;

[RequireComponent(typeof(StageCameraController))]
[RequireComponent(typeof(StageUIManager))]
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("UI Base Prefab")]
    public GameObject selectPanel; 

    [Header("캐시 없을 때")]
    [Tooltip("서버가 S_STAGE_INFO를 안 보내도 SelectPanel·입장 UI를 켤지 (로컬 StageInfo)")]
    [SerializeField] private bool useLocalStageInfoWhenServerCacheEmpty = true;

    [Header("Nodes & Environment")] 
    public List<StageNode> stageNodes = new List<StageNode>();
    public bool isMovementPaused = false;

    private StageCameraController _cameraController;
    private StageUIManager _uiManager;
    
    private StageNode _currentSelectedNode;
    private bool _isTransitioning = false;
    private GameObject[] _clickOffObjects;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cameraController = GetComponent<StageCameraController>();
        _uiManager = GetComponent<StageUIManager>();
    }

    private void Start()
    {
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
            PacketHandler.Instance.OnStageInfoEvent -= OnStageInfoReceived;
        }
    }

    private void OnStageInfoReceived(S_STAGE_INFO packet)
    {
        int n = packet?.Stages?.Count ?? 0;
        Debug.Log($"[StageManager] S_STAGE_INFO 갱신: {n}개 — 노드(Chapter,Stage)를 서버 값과 맞췄는지 확인하세요.");

        if (n > 0 && PacketDispatcher.Instance != null)
            PacketDispatcher.Instance.SendGetClearInfo();
    }

    // 서버에서 클리어 정보가 도착했을 때 실행되는 함수
    private void HandleGetClearInfoResponse(S_GET_CLEAR_INFO packet)
    {
        // 1. 서버에서 온 데이터를 MapId 기준으로 딕셔너리에 정리
        Dictionary<int, bool> clearDataDict = new Dictionary<int, bool>();
        foreach (var clearInfo in packet.StageClears)
        {
            clearDataDict[clearInfo.MapId] = true;
        }

        // 2. 모든 행성(Node)을 순회하면서 클리어 여부 업데이트
        foreach (var node in stageNodes)
        {
            if (node == null) continue;

            if (DbCacheManager.TryGetStageInfoByChapterStage(node.stageLevel, node.stageIndex, out StageInfo info))
            {
                // 이 행성의 MapId가 클리어 목록에 있는지 확인
                bool isCleared = clearDataDict.ContainsKey(info.MapId);
                
                // StageNode의 상태를 업데이트하고 시각적 테두리(주황색) 적용
                node.isClearedStage = isCleared; 
                node.SetClearState(isCleared);
            }
        }
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

        if (_currentSelectedNode != null && !_isTransitioning && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            StartCoroutine(ClosePanelSequence());
        }
    }
    
    public void EnterSelectedStage()
    {
        if (_currentSelectedNode == null) return;

        int level = _currentSelectedNode.stageLevel;
        int index = _currentSelectedNode.stageIndex;

        // MapId·Chapter·Stage는 서버 DB와 한 세트. 캐시의 StageInfo 기준으로 보낸다(C_START_STAGE의 StageIndex = StageInfo.Stage).
        if (DbCacheManager.TryGetStageInfoByChapterStage(level, index, out StageInfo info))
        {
            Debug.Log(
                $"[StageManager] C_START_STAGE 전송 MapId={info.MapId}, Chapter={info.Chapter}, StageIndex(=Stage)={info.Stage}");
            PacketDispatcher.Instance.SendStartStage(info.MapId, info.Chapter, info.Stage);
        }
    }
    
    private void HandleStartStageResponse(S_START_STAGE packet)
    {
        if (packet.Success)
        {
            Debug.Log($"[StageManager] 스테이지 시작 허가됨! 씬 이동 준비: {packet.Stage.StageName}");
            
            SceneLoader.Instance.LoadScene(Define.Scene.GAME_1_1); 
        }
        else
        {
            Debug.LogWarning("[StageManager] 서버가 스테이지 시작을 거절했습니다!");
            // (필요하다면 여기에 "입장 실패" 경고 팝업을 띄우는 로직 추가)
        }
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
            if (!DbCacheManager.HasStageInfo && useLocalStageInfoWhenServerCacheEmpty)
            {
                stageInfo = BuildLocalFallbackStageInfo(clickedNode);
                Debug.LogWarning(
                    "[StageManager] 서버 S_STAGE_INFO가 없어 로컬 폴백으로 패널을 띄웁니다. " +
                    "StageNode의 localMapIdOverride를 실제 map_id에 맞추면 서버 스타트가 안정적입니다. " +
                    $"(임시 MapId={stageInfo.MapId})");
                DbCacheManager.MergeStageInfoEntry(stageInfo);
            }
            else if (!DbCacheManager.HasStageInfo)
            {
                Debug.LogWarning(
                    "[StageManager] S_STAGE_INFO가 아직 캐시에 없습니다. " +
                    "로그인·서버 응답을 기다리거나, 잠시 후 다시 누르세요. (필요 시 DB 재요청을 보냅니다.) " +
                    "또는 StageManager의 '캐시 없을 때' 로컬 폴백을 켜세요.");
                DbCacheManager.RequestDbData();
                _currentSelectedNode = null;
                return;
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

        StartCoroutine(OpenPanelSequence(_currentSelectedNode, stageInfo));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode, StageInfo stageInfo)
    {
        _isTransitioning = true;
        isMovementPaused = true; 

        ToggleFocusMode(targetNode, true);
        _uiManager.ToggleNavButtons(false);

        yield return StartCoroutine(_cameraController.ZoomIn(targetNode.transform));

        if (selectPanel != null)
        {
            // UI 매니저에게 targetNode.isClearedStage (클리어 여부)를 같이 넘겨줌
            yield return StartCoroutine(_uiManager.OpenPanel(
                selectPanel,
                stageInfo.StageName,
                stageInfo.Difficulty,
                stageInfo.Description,
                stageInfo.EstimatedClearTime,
                targetNode.isClearedStage)); 
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

        _uiManager.ToggleNavButtons(true);
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