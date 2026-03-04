using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Environment Objects")]
    public GameObject orbitCenterObject;
    
    [Header("OrbitLines")] [SerializeField]
    private List<OrbitLineRenderer> _allOrbitLines = new List<OrbitLineRenderer>();

    [Header("Nodes")] public List<StageNode> stageNodes = new List<StageNode>();
    public bool isMovementPaused = false;

    [Header("UI Navigation Buttons")] [SerializeField]
    private GameObject leftButton;

    [SerializeField] private GameObject rightButton;

    [Header("UI Pop-Up Settings")] [SerializeField]
    private float popUpDuration = 0.2f;

    [SerializeField] private Vector3 finalPanelScale = new Vector3(1f, 1f, 1f);

    [Header("Camera Move Settings")] [Tooltip("카메라가 이동하는 데 걸리는 시간(초)")] [SerializeField]
    private float cameraMoveDuration = 0.5f;

    [Tooltip("줌인 시 카메라의 고정 회전 각도")] [SerializeField]
    private Vector3 zoomEulerAngles = new Vector3(45f, 0f, 0f);

    [Tooltip("행성 중심으로부터 카메라가 떨어질 거리")] [SerializeField]
    private float zoomDistance = 10f;

    private Vector3 originPos = new Vector3(0.2f, -13.3f, 6.2f);
    private Quaternion originRot = new Quaternion(-0.3888f, 0.0016f, 0.003f,0.92f);

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
        StartCoroutine(OpenPanelSequence(clickedNode));
    }

    private IEnumerator OpenPanelSequence(StageNode targetNode)
    {
        _isTransitioning = true;
        isMovementPaused = true;

        // =========================================================
        // [핵심] 집중 모드 ON: 선택한 행성 빼고 다 숨기기!
        // =========================================================
        ToggleFocusMode(targetNode, true);

        if (leftButton != null) leftButton.SetActive(false);
        if (rightButton != null) rightButton.SetActive(false);

        if (_mainCamera != null)
        {
            Quaternion dynamicTargetRot = Quaternion.Euler(zoomEulerAngles);
            Vector3 dynamicTargetPos =
                targetNode.transform.position - (dynamicTargetRot * Vector3.forward * zoomDistance);

            StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, dynamicTargetPos,
                dynamicTargetRot));
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
            StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, originPos,
                originRot));
        }

        if (_currentPanel != null)
        {
            yield return StartCoroutine(DynamicClosePanel(_currentPanel));
            Destroy(_currentPanel);
            _currentPanel = null;
        }

        // =========================================================
        // [핵심] 집중 모드 OFF: 숨겼던 친구들 다시 다 보여주기
        // =========================================================
        ToggleFocusMode(null, false);

        isMovementPaused = false;
        _currentSelectedNode = null;
        _isTransitioning = false;

        if (leftButton != null) leftButton.SetActive(true);
        if (rightButton != null) rightButton.SetActive(true);
    }

    // =========================================================
    // 환경 오브젝트(행성, 선, 태양) 껐다 켜는 함수
    // =========================================================
    private void ToggleFocusMode(StageNode targetNode, bool isFocusing)
    {
        // 1. 중앙 태양(Center) 숨기기/보이기
        if (orbitCenterObject != null)
        {
            orbitCenterObject.SetActive(!isFocusing);
        }

        // 2. 모든 궤도 선(Line) 숨기기/보이기
        foreach (var line in _allOrbitLines)
        {
            if (line != null) line.gameObject.SetActive(!isFocusing);
        }

        // 3. 행성들 처리
        foreach (var node in stageNodes)
        {
            if (node == null) continue;

            if (isFocusing)
            {
                // 집중 모드일 때: 타겟 행성만 남기고 나머지는 끔
                if (node == targetNode) node.gameObject.SetActive(true);
                else node.gameObject.SetActive(false);
            }
            else
            {
                // 복귀 모드일 때: 모든 행성을 다시 켬
                node.gameObject.SetActive(true);
            }
        }
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