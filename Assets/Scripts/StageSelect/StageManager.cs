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

    [Header("UI Navigation Buttons")]
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;

    [Header("UI Pop-Up Settings")]
    [SerializeField] private float popUpDuration = 0.2f;
    [SerializeField] private Vector3 finalPanelScale = new Vector3(1f, 1f, 1f); 

    [Header("Camera Move Settings")]
    [Tooltip("카메라가 이동하는 데 걸리는 시간(초)")]
    [SerializeField] private float cameraMoveDuration = 0.5f;

    private Vector3 originPos = new Vector3(0.200000003f, -10.3100004f, 7.5999999f);
    private Quaternion originRot = new Quaternion(-0.390727788f, 0.0016025817f, 0.00377544644f, 0.920497179f);

    private Vector3 targetPos = new Vector3(4.94000006f, -12.2399998f, 4f);
    private Quaternion targetRot = new Quaternion(-0.344169676f, -0.0222564563f, -0.0605728365f, 0.936687112f);

    private Camera _mainCamera;
    private GameObject _currentPanel;
    private StageNode _currentSelectedNode; 
    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _mainCamera = Camera.main;
    }

    private void Start()
    {
        if (stageNodes.Count == 0)
            stageNodes = new List<StageNode>(Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None));

        foreach (var node in stageNodes)
        {
            node.Init();
        }

        if (_mainCamera != null)
        {
            _mainCamera.transform.position = originPos;
            _mainCamera.transform.rotation = originRot;
        }
    }

    private void Update()
    {
        foreach (var node in stageNodes)
        {
            if (node != null)
            {
                node.UpdateScale(Time.deltaTime);
                if (!isMovementPaused) node.UpdateMovement(Time.deltaTime);
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
        StartCoroutine(OpenPanelSequence(clickedNode));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode)
    {
        _isTransitioning = true;
        isMovementPaused = true; 

        // 1. 카메라가 이동하기 전에 좌/우 버튼 비활성화 (숨기기)
        if (leftButton != null) leftButton.SetActive(false);
        if (rightButton != null) rightButton.SetActive(false);

        if (_mainCamera != null)
        {
            StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, targetPos, targetRot));
        }

        if (targetNode.stagePanelPrefab != null)
        {
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

        if (_mainCamera != null)
        {
            StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, originPos, originRot));
        }

        if (_currentPanel != null)
        {
            yield return StartCoroutine(DynamicClosePanel(_currentPanel));
            Destroy(_currentPanel);
            _currentPanel = null;
        }

        isMovementPaused = false; 
        _currentSelectedNode = null;
        _isTransitioning = false;

        // 2. 패널이 완전히 닫히고 카메라가 복귀한 뒤에 버튼 다시 활성화
        if (leftButton != null) leftButton.SetActive(true);
        if (rightButton != null) rightButton.SetActive(true);
    }

    private IEnumerator MoveCamera(Vector3 startP, Quaternion startR, Vector3 endP, Quaternion endR)
    {
        float elapsed = 0f;
        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / cameraMoveDuration);
            
            _mainCamera.transform.position = Vector3.Lerp(startP, endP, t);
            _mainCamera.transform.rotation = Quaternion.Slerp(startR, endR, t);
            
            yield return null;
        }
        
        _mainCamera.transform.position = endP;
        _mainCamera.transform.rotation = endR;
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