using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Nodes")]
    public List<StageNode> stageNodes = new List<StageNode>();
    public bool isMovementPaused = false;

    [Header("UI Pop-Up Settings")]
    [SerializeField] private float popUpDuration = 0.2f;
    [SerializeField] private Vector3 finalPanelScale = new Vector3(1f, 1f, 1f); 

    private GameObject _currentPanel;
    private StageNode _currentSelectedNode; 
    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (stageNodes.Count == 0)
            stageNodes = new List<StageNode>(Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None));

        foreach (var node in stageNodes)
        {
            node.Init();
        }
    }

    private void Update()
    {
        // 행성들 궤도/자전 업데이트 (isMovementPaused가 false일 때만 움직임)
        foreach (var node in stageNodes)
        {
            if (node != null)
            {
                node.UpdateScale(Time.deltaTime);
                if (!isMovementPaused) node.UpdateMovement(Time.deltaTime);
            }
        }

        // ESC 키로 창 닫기
        if (_currentSelectedNode != null && !_isTransitioning && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            StartCoroutine(ClosePanelSequence());
        }
    }

    public void OnStageClicked(StageNode clickedNode)
    {
        if (_currentSelectedNode != null || _isTransitioning) return;

        _currentSelectedNode = clickedNode;
        StartCoroutine(OpenPanelSequence(clickedNode));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode)
    {
        _isTransitioning = true;

        // 팝업이 떠 있는 동안 배경 행성들 움직임 정지 (어지러움 방지)
        isMovementPaused = true; 

        if (targetNode.stagePanelPrefab != null)
        {
            // Canvas 프리팹 화면 최상단에 바로 생성
            _currentPanel = Instantiate(targetNode.stagePanelPrefab);
            
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

        _isTransitioning = false;
    }

    // 외부 UI 버튼(X 버튼 등)에서도 호출할 수 있도록 public으로 열어둠
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

        if (_currentPanel != null)
        {
            yield return StartCoroutine(DynamicClosePanel(_currentPanel));
            Destroy(_currentPanel);
            _currentPanel = null;
        }

        // 창이 닫히면 배경 행성들 다시 움직임 재개
        isMovementPaused = false; 

        _currentSelectedNode = null;
        _isTransitioning = false;
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