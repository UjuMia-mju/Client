using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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

        // MOCK 서버 호출
        MockServerResponse(clickedNode.stageLevel, clickedNode.stageIndex);
        
        // TODO: 서버 구현 시 주석 해제
        // NetManager.Instance.SendShowStage(clickedNode.stageLevel, clickedNode.stageIndex);
    }

    // TODO: 서버 구현 시 주석 해제
    private void MockServerResponse(int stageLevel, int stageIndex)
    {
        Debug.Log($"[Mock] 서버에 Level {stageLevel}, Index {stageIndex} 정보 요청...");
        
        string mockStageName = $"챕터 {stageLevel}-{stageIndex}";
        int mockDifficulty = Mathf.Clamp(stageLevel + stageIndex, 1, 5); 
        string mockDescription = "거대한 수풀과 숲을 이룬 버섯 군락이 지표면을 완전히 점령한 행성입니다. \n\n이곳의 식물들은 비정상적으로 거대하여, " +
                                 "\n한 그루의 높이가 수 킬로미터에 달합니다. \n\n대기 중에는 고농도의 산소와 포자가 가득 차 있어, " +
                                 "모든 유기물은 일반적인 환경보다 수십 배 빠른 속도로 성장합니다.";
        
        OnReceiveStageInfo(mockStageName, mockDifficulty, mockDescription);
    }

    public void OnReceiveStageInfo(string stageName, int difficulty, string description)
    {
        StartCoroutine(OpenPanelSequence(_currentSelectedNode, stageName, difficulty, description));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode, string stageName, int difficulty, string description)
    {
        _isTransitioning = true;
        isMovementPaused = true; 

        ToggleFocusMode(targetNode, true);
        _uiManager.ToggleNavButtons(false);

        yield return StartCoroutine(_cameraController.ZoomIn(targetNode.transform));

        if (selectPanel != null)
        {
            yield return StartCoroutine(_uiManager.OpenPanel(selectPanel, stageName, difficulty, description));
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