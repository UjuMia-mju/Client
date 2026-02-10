using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// MenuPanel에서의 다양한 UI 연출
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    
    // key
    private Key closeKey = Key.Escape;
    
    // Zoom In
    private Vector3 originPos = new Vector3(0f, 1f, -900f);
    private Vector3 originRot = new Vector3(0f, 0f, 0f);
    private float zoomDuration = 0.5f;
    private float targetDistance = 600f;
    
    // Menu Panel
    private GameObject _currentSubPanel;
    private MenuPanelController _menuPanelController;
    
    // Pop - Up
    private float popUpDuration = 0.2f;
    private Vector3 finalPanelScale = new Vector3(0.005f, 0.005f, 1f);
    
    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        _menuPanelController = Object.FindFirstObjectByType<MenuPanelController>();
    }

    private void Update()
    {
        // 키를 누르면 정해진 originPos로 복귀
        if (Keyboard.current[closeKey].wasPressedThisFrame && _currentSubPanel != null)
        {
            BackToMainMenu();
        }
    }

    public void StartZoomSequence(Transform targetTransform, GameObject panelPrefab)
    {
        StartCoroutine(ZoomIn(targetTransform, panelPrefab));
    }

    private IEnumerator ZoomIn(Transform target, GameObject panelPrefab)
    {
        // 1. 카메라 이동 로직 (생략)
        Vector3 targetPos = target.position - (mainCamera.transform.forward * targetDistance);
        Quaternion targetRot = Quaternion.LookRotation(target.position - targetPos);

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / zoomDuration);
            mainCamera.transform.position = Vector3.Lerp(originPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(Quaternion.Euler(originRot), targetRot, t);
            yield return null;
        }

        // 2. 패널 생성 로직
        if (panelPrefab != null)
        {
            _currentSubPanel = Instantiate(panelPrefab, canvas.transform);
        
            // 패널의 시작 스케일을 0으로 설정하여 작게 시작
            // FinalScale 변수를 사용하여 최종 스케일을 지정
            _currentSubPanel.transform.localScale = Vector3.zero; 

            // 위치 및 회전 고정
            float distanceInFrontOfCamera = 2.0f; 
            _currentSubPanel.transform.position = mainCamera.transform.position + (mainCamera.transform.forward * distanceInFrontOfCamera);
            _currentSubPanel.transform.rotation = mainCamera.transform.rotation;

            // 다이내믹 팝업 효과
            StartCoroutine(DynamicPopUpPanel(_currentSubPanel));
        }
    }

    public void BackToMainMenu()
    {
        if (_currentSubPanel != null)
        {
            // Fade Out
            StartCoroutine(ClosePanelSequence());
        }
    }
    
    /// <summary>
    /// 패널을 닫는 과정
    /// 단계: Fade Out -> Destroy -> Camera Return
    /// </summary>
    private IEnumerator ClosePanelSequence()
    {
        // 1. 패널 Fade Out 및 축소 연출 실행
        yield return StartCoroutine(DynamicClosePanel(_currentSubPanel));

        // 2. 연출이 끝난 후 패널 제거
        Destroy(_currentSubPanel);
        _currentSubPanel = null;

        // 3. 카메라 복귀 시작
        StartCoroutine(ReturnToOrigin());
    }
    
    // closeKey를 누르면 원래 화면으로 되돌아감
    private IEnumerator ReturnToOrigin()
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        Quaternion targetOriginRot = Quaternion.Euler(originRot);

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / zoomDuration);

            // 현재 위치에서 변수로 정해둔 originPos로 복귀
            mainCamera.transform.position = Vector3.Lerp(startPos, originPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetOriginRot, t);
            yield return null;
        }

        if (_menuPanelController != null) _menuPanelController.ResetAllButtons();
    }
    
    /// <summary>
    /// 패널이 나타나는 다이내믹 연출 코루틴
    /// </summary>
    private IEnumerator DynamicPopUpPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();

        cg.alpha = 0f; // 처음엔 투명
        panel.transform.localScale = Vector3.zero; // 처음엔 스케일 0

        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration); // 부드러운 가감속

            // 투명도 (0 -> 1)
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            // 스케일 (0 -> finalPanelScale)
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, finalPanelScale, t);
        
            yield return null;
        }

        // 연출 완료 후 최종 값으로 고정
        cg.alpha = 1f;
        panel.transform.localScale = finalPanelScale;
    }
    
    /// <summary>
    /// 패널이 사라지는 다이내믹 연출 코루틴
    /// </summary>
    private IEnumerator DynamicClosePanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();

        Vector3 startScale = panel.transform.localScale;
        float elapsed = 0f;

        while (elapsed < popUpDuration) // Pop-Up과 동일한 popUpDuration 사용
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration);

            // 투명도 (1 -> 0)
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            
            yield return null;
        }

        cg.alpha = 0f;
        panel.transform.localScale = Vector3.zero;
    }
}