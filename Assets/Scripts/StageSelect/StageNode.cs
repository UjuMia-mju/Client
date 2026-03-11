using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class StageNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Stage Identity")]
    [Tooltip("이 행성의 고유 스테이지 ID")]
    public int stageID; 

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

    public void UpdateMovement(float deltaTime, bool isGlobalPaused)
    {
        // 마우스를 올리고 있을 때는 클릭하기 쉽게 완전히 멈춤
        if (_isHovered) return;

        // 자전: 전체 일시정지 상태와 무관하게 항상 돎
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);

        // 공전: 전체 일시정지(줌인) 상태가 아닐 때만 궤도를 따라 돎
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
        // 줌인 상태일 때는 호버 이벤트 무시
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = true; 
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 줌인 상태일 때는 호버 이벤트 무시
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 줌인 상태일 때는 중복 클릭 방지
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageClicked(this);
        }
    }
}