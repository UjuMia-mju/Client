using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Protocol;

[RequireComponent(typeof(StageCameraController))]
[RequireComponent(typeof(StageUIManager))]
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("UI Base Prefab")]
    public GameObject selectPanel; 

    [Header("Nodes & Environment")] 
    public List<StageNode> stageNodes = new List<StageNode>();
    public bool isMovementPaused = false;

    private StageCameraController _cameraController;
    private StageUIManager _uiManager;
    
    private StageNode _currentSelectedNode;
    private bool _isTransitioning = false;
    private bool _gameplaySceneLoadStarted;
    private GameObject[] _clickOffObjects;

    /// <summary>맵 ID별 클리어 별 개수(0~3). S_GET_CLEAR_INFO 기준.</summary>
    private readonly Dictionary<int, int> _clearStarCountByMapId = new Dictionary<int, int>();

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
        }
        PacketDispatcher.Instance.SendGetClearInfo();
    }

    private void OnDestroy()
    {
        if (PacketHandler.Instance != null) 
        {
            PacketHandler.Instance.OnGetClearInfoEvent -= HandleGetClearInfoResponse;
            PacketHandler.Instance.OnStartStageEvent -= HandleStartStageResponse;
        }
    }

    private void HandleGetClearInfoResponse(S_GET_CLEAR_INFO packet)
    {
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

            if (DbCacheManager.Instance.TryGetStageInfoByChapterStage(node.stageLevel, node.stageIndex, out StageInfo info))
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

        if (DbCacheManager.Instance.TryGetStageInfoByChapterStage(level, index, out StageInfo info))
        {
            if (info.Chapter != level || info.Stage != index)
            {
                Debug.LogWarning(
                    $"[StageManager] 노드({level},{index})와 StageInfo({info.Chapter},{info.Stage}) 불일치. StageInfo 기준으로 전송합니다.");
            }

            Debug.Log($"[StageManager] 스테이지 시작 요청 MapId={info.MapId}, Chapter={info.Chapter}, Stage={info.Stage}");
            PacketDispatcher.Instance.SendStartStage(info.MapId, info.Chapter, info.Stage);
        }
    }
    
    private void HandleStartStageResponse(S_START_STAGE packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning("[StageManager] 서버가 스테이지 시작을 거절했습니다.");
            return;
        }

        if (_gameplaySceneLoadStarted)
            return;

        _gameplaySceneLoadStarted = true;

        int mapId = 1;
        int chapter = 1;
        int stageNum = 1;

        if (packet.Stage != null)
        {
            mapId = packet.Stage.MapId;
            chapter = packet.Stage.Chapter;
            stageNum = packet.Stage.Stage;
        }
        else if (_currentSelectedNode != null &&
                 DbCacheManager.Instance.TryGetStageInfoByChapterStage(
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
                $"[StageManager] Define.Scene에 없는 스테이지입니다. map={mapId}, chapter={chapter}, stage={stageNum}. " +
                $"fallback={Define.Scene.GAME_1_1}");
            sceneName = Define.Scene.GAME_1_1;
        }

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void OnStageClicked(StageNode clickedNode)
    {
        if (_currentSelectedNode != null || _isTransitioning) return;
        _currentSelectedNode = clickedNode;

        if (!DbCacheManager.Instance.TryGetStageInfoByChapterStage(
                clickedNode.stageLevel,
                clickedNode.stageIndex,
                out StageInfo stageInfo))
        {
            Debug.LogWarning($"[StageManager] 캐시에 스테이지 정보가 없습니다. chapter={clickedNode.stageLevel}, stage={clickedNode.stageIndex}");
            _currentSelectedNode = null;
            return;
        }

        OnReceiveStageInfo(stageInfo);
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
