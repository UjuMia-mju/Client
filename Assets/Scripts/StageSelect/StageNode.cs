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

    // StageManager가 게임 시작 시 한 번 호출해 주는 초기화 함수
    public void Init()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;

        // 랜덤 위치 시작
        if (randomizeStartPosition && orbitCenter != null)
        {
            float randomAngle = Random.Range(0f, 360f);
            transform.RotateAround(orbitCenter.position, orbitAxis, randomAngle);
        }
    }

    // 이동 로직
    public void UpdateMovement(float deltaTime)
    {
        if (orbitCenter != null) transform.RotateAround(orbitCenter.position, orbitAxis, orbitSpeed * deltaTime);
        transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);
    }

    // 크기 로직
    public void UpdateScale(float deltaTime)
    {
        if (transform.localScale != _targetScale)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, deltaTime * hoverTransitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData) { _targetScale = _originalScale * hoverScaleMultiplier; }
    public void OnPointerExit(PointerEventData eventData) { _targetScale = _originalScale; }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (stagePanelPrefab != null)
        {
            StageManager.Instance.OnStageClicked(this);
        }
    }
}