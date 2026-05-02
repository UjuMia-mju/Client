using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private UIPanelAnimator animator; // 애니메이터 참조
    
    // Zoom Settings
    private Vector3 originPos = new Vector3(0f, 1f, -900f);
    private Vector3 originRot = new Vector3(0f, 0f, 0f);
    private float zoomDuration = 0.5f;
    private float targetDistance = 600f;
    
    // Panel Settings
    private GameObject _currentSubPanel;
    private MenuPanelController _menuPanelController;
    private Vector3 finalPanelScale = new Vector3(0.005f, 0.005f, 1f); // 특정 스케일값 유지
    
    private Key closeKey = Key.Escape;
    private bool isTransitioning = false; // 중복 실행 방지

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (animator == null) animator = GetComponent<UIPanelAnimator>();
        if (animator == null) animator = UIPanelAnimator.Instance;
        _menuPanelController = Object.FindFirstObjectByType<MenuPanelController>();
    }

    private void Update()
    {
        if (Keyboard.current[closeKey].wasPressedThisFrame && _currentSubPanel != null && !isTransitioning)
        {
            BackToMainMenu();
        }
    }

    public void StartZoomSequence(Transform targetTransform, GameObject panelPrefab)
    {
        if (isTransitioning) return;
        StartCoroutine(ZoomIn(targetTransform, panelPrefab));
    }

    private IEnumerator ZoomIn(Transform target, GameObject panelPrefab)
    {
        isTransitioning = true;

        // 1. 카메라 이동 로직
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

        // 2. 패널 생성 및 연출 (UIPanelAnimator 활용)
        if (panelPrefab != null)
        {
            _currentSubPanel = Instantiate(panelPrefab, canvas.transform);
            
            // 위치/회전 설정
            float distanceInFrontOfCamera = 2.0f; 
            _currentSubPanel.transform.position = mainCamera.transform.position + (mainCamera.transform.forward * distanceInFrontOfCamera);
            _currentSubPanel.transform.rotation = mainCamera.transform.rotation;

            // 공용 애니메이터로 팝업 연출 실행
            yield return StartCoroutine(animator.FadeIn(_currentSubPanel, finalPanelScale));
        }

        isTransitioning = false;
    }

    public void BackToMainMenu()
    {
        if (_currentSubPanel != null) StartCoroutine(ClosePanelSequence());
    }
    
    private IEnumerator ClosePanelSequence()
    {
        isTransitioning = true;
        SoundManager.Instance.PlaySFX("Click3");
        
        // 1. 공용 애니메이터로 패널 닫기 연출 (애니메이터 내부에서 Destroy까지 수행됨)
        // 주의: 현재 UIPanelAnimator.FadeOut은 내부에서 Destroy(panel)를 호출함
        yield return StartCoroutine(animator.FadeOut(_currentSubPanel));
        _currentSubPanel = null;

        // 2. 카메라 복귀
        yield return StartCoroutine(ReturnToOrigin());
        
        isTransitioning = false;
    }
    
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
            mainCamera.transform.position = Vector3.Lerp(startPos, originPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetOriginRot, t);
            yield return null;
        }

        if (_menuPanelController != null) _menuPanelController.ResetAllButtons();
    }
}