using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class StageNode : MonoBehaviour
{
    [Header("Orbit (공전) Settings")]
    public Transform orbitCenter; 
    public Vector3 orbitAxis = Vector3.up; 
    public float orbitSpeed = 10f; 

    [Header("Spin (자전) Settings")]
    public Vector3 spinAxis = Vector3.up; 
    public float spinSpeed = 50f; 

    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.2f; 
    [SerializeField] private float hoverTransitionSpeed = 10f; 

    [Header("UI Interaction")]
    [SerializeField] private GameObject stagePanelPrefab; 
    
    private MenuManager _menuManager;
    private StageManager _stageManager; // 매니저 참조 추가
    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Start()
    {
        _menuManager = Object.FindFirstObjectByType<MenuManager>();
        _stageManager = Object.FindFirstObjectByType<StageManager>();
        
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    // StageManager가 호출하는 이동 로직
    public void UpdateMovement(float deltaTime)
    {
        if (orbitCenter != null)
        {
            transform.RotateAround(orbitCenter.position, orbitAxis, orbitSpeed * deltaTime);
        }
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);
    }

    // StageManager가 호출하는 크기 변환(Hover) 로직
    public void UpdateScale(float deltaTime)
    {
        if (transform.localScale != _targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, deltaTime * hoverTransitionSpeed);
        }
    }

    private void OnMouseEnter()
    {
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        _targetScale = _originalScale;
    }

    private void OnMouseDown()
    {
        if (_menuManager != null && stagePanelPrefab != null)
        {
            // 1. 카메라 줌인 & 패널 팝업
            _menuManager.StartZoomSequence(this.transform, stagePanelPrefab);
            
            // 2. 패널이 열렸으므로 전체 행성의 움직임 일시 정지 (어지러움 방지)
            if (_stageManager != null)
            {
                _stageManager.SetMovementPause(true);
            }
        }
    }
}