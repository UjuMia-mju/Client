using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class StageNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Orbit & Spin Settings")]
    public Transform orbitCenter; 
    public Vector3 orbitAxis = new Vector3(0, 0, 1); 
    public float orbitSpeed = 10f; 
    public Vector3 spinAxis = Vector3.up; 
    public float spinSpeed = 50f; 
    public bool randomizeStartPosition = true; 

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.5f; 
    public float hoverTransitionSpeed = 10f; 

    [Header("UI Interaction")]
    public GameObject stagePanelPrefab; 
    
    private Vector3 _originalScale;
    private Vector3 _targetScale;
    private bool _isHovered = false;

    public void Init()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;

        if (randomizeStartPosition && orbitCenter != null)
        {
            float randomAngle = Random.Range(0f, 360f);
            transform.RotateAround(orbitCenter.position, orbitAxis, randomAngle);
        }
    }

    // 변경된 핵심 부분: 외부에서 '전체 일시정지' 상태를 받아옴
    public void UpdateMovement(float deltaTime, bool isGlobalPaused)
    {
        // 1. 마우스 호버 시에는 클릭을 위해 '완전 정지' (자전도 멈춤)
        if (_isHovered) return;

        // 2. 자전(Spin): 전체 일시정지(isGlobalPaused)와 상관없이 항상 돔!
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);

        // 3. 공전(Orbit): 전체 일시정지가 아닐 때만 돔
        if (!isGlobalPaused && orbitCenter != null)
        {
            transform.RotateAround(orbitCenter.position, orbitAxis, orbitSpeed * deltaTime);
        }
    }

    public void UpdateScale(float deltaTime)
    {
        if (transform.localScale != _targetScale)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, deltaTime * hoverTransitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = true; 
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        if (stagePanelPrefab != null)
        {
            _isHovered = false; 
            _targetScale = _originalScale;
            
            StageManager.Instance.OnStageClicked(this);
        }
    }
}