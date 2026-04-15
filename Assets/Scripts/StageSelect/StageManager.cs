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
    [Tooltip("여기에 SelectPanel 프리팹을 넣어주세요!")]
    public GameObject selectPanel; 

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

        // 패킷 핸들러에서 클리어 정보 이벤트를 구독
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnGetClearInfoEvent += HandleGetClearInfoResponse;
        }
        
        PacketDispatcher.Instance.SendGetClearInfo();
    }

    // 씬이 파괴될 때 메모리 누수 방지를 위해 구독 해제
    private void OnDestroy()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnGetClearInfoEvent -= HandleGetClearInfoResponse;
        }
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

            if (DbCacheManager.Instance.TryGetStageInfoByChapterStage(node.stageLevel, node.stageIndex, out StageInfo info))
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

    public void ClosePanel()
    {
        if (_currentSelectedNode != null && !_isTransitioning)
        {
            StartCoroutine(ClosePanelSequence());
        }
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