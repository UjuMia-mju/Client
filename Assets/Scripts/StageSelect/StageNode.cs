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

    public void UpdateMovement(float deltaTime)
    {
        // 마우스가 올라가 있으면(Hover 상태) 공전과 자전을 모두 멈춤
        if (_isHovered) return;

        if (orbitCenter != null) transform.RotateAround(orbitCenter.position, orbitAxis, orbitSpeed * deltaTime);
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);
    }

    public void UpdateScale(float deltaTime)
    {
        if (transform.localScale != _targetScale)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, deltaTime * hoverTransitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true; 
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false; 
        _targetScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (stagePanelPrefab != null)
        {
            _isHovered = false; 
            _targetScale = _originalScale;
            
            StageManager.Instance.OnStageClicked(this);
        }
    }
}