using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviorSingleton<MenuManager>
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    
    private Vector3 originPos = new Vector3(0f, 1f, -900f);
    private Vector3 originRot = new Vector3(0f, 0f, 0f);
    private float zoomDuration = 0.8f;
    private float targetDistance = 600f;
    

    private GameObject _currentSubPanel;
    private MenuPanelController _menuPanelController;

    protected override void Awake()
    {
        base.Awake();
        if (mainCamera == null) mainCamera = Camera.main;
        _menuPanelController = Object.FindFirstObjectByType<MenuPanelController>();
    }

    private void Update()
    {
        // ESC 누르면 정해진 originPos로 복귀
        if (Keyboard.current.escapeKey.wasPressedThisFrame && _currentSubPanel != null)
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
            
            _currentSubPanel.transform.localScale = new Vector3(0.005f, 0.005f, 1f);

            // 3. 위치는 카메라 앞으로 지정 (이동 로직은 그대로 유지)
            float distanceInFrontOfCamera = 2.0f; 
            _currentSubPanel.transform.position = mainCamera.transform.position + (mainCamera.transform.forward * distanceInFrontOfCamera);
            _currentSubPanel.transform.rotation = mainCamera.transform.rotation;
        }
    }

    public void BackToMainMenu()
    {
        if (_currentSubPanel != null)
        {
            Destroy(_currentSubPanel);
            _currentSubPanel = null;
            StartCoroutine(ReturnToOrigin());
        }
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

            // 현재 위치에서 변수로 정해둔 originPos로 복귀
            mainCamera.transform.position = Vector3.Lerp(startPos, originPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetOriginRot, t);
            yield return null;
        }

        if (_menuPanelController != null) _menuPanelController.ResetAllButtons();
    }
}