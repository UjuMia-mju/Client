using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class StageNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
    private StageManager _stageManager;
    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Start()
    {
        _menuManager = Object.FindFirstObjectByType<MenuManager>();
        _stageManager = Object.FindFirstObjectByType<StageManager>();
        
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    public void UpdateMovement(float deltaTime)
    {
        if (orbitCenter != null) transform.RotateAround(orbitCenter.position, orbitAxis, orbitSpeed * deltaTime);
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);
    }

    public void UpdateScale(float deltaTime)
    {
        if (transform.localScale != _targetScale)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, deltaTime * hoverTransitionSpeed);
    }

    // 마우스가 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    // 마우스가 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }

    // 클릭했을 때
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_menuManager != null && stagePanelPrefab != null)
        {
            _menuManager.StartZoomSequence(this.transform, stagePanelPrefab);
            if (_stageManager != null) _stageManager.SetMovementPause(true);
        }
    }
}