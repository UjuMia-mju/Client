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
    private float targetDistance = 400f;
    

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
        // 1. 카메라 이동 로직 (기존과 동일)
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

        // 2. 카메라가 도착한 후, 카메라 바로 앞에 패널 생성
        if (panelPrefab != null)
        {
            _currentSubPanel = Instantiate(panelPrefab, canvas.transform);

            // [핵심] 패널의 위치를 현재 카메라 위치에서 앞쪽으로 살짝 띄워서 배치
            // 2.0f는 카메라와 패널 사이의 간격입니다. 환경에 맞춰 조절하세요.
            float distanceInFrontOfCamera = 2.0f; 
            _currentSubPanel.transform.position = mainCamera.transform.position + (mainCamera.transform.forward * distanceInFrontOfCamera);
        
            // 패널이 카메라를 정면으로 바라보게 회전값 동기화
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