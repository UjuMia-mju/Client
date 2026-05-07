using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class StageNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Stage Identity")]
    public int stageLevel = 1; 
    public int stageIndex = 1;

    [Header("로컬 폴백 (서버 S_STAGE_INFO가 없을 때)")]
    [Tooltip("0이면 MapId = stageLevel*100+stageIndex 로 임시 사용. 실제 DB map_id를 알면 여기에 넣으세요.")]
    public int localMapIdOverride;
    [Tooltip("비어 있으면 '스테이지 (chapter)-(stage)' 형식")]
    public string localDisplayName = "";
    
    [Tooltip("이 체크박스를 켜면 클리어 테두리가 나타납니다.")]
    public bool isClearedStage = false; 

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
    
    [Header("Clear Outline Settings")]
    [Tooltip("이 행성에 붙어있는 아웃라인 컴포넌트를 넣어주세요.")]
    public Outline nodeOutline; 
    
    [Tooltip("미클리어 상태)")]
    public Color defaultColor = Color.white;
    
    [Tooltip("클리어 상태")]
    public Color clearedColor = new Color(1.0f, 0.5f, 0.0f); // Orange

    public void Init()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;

        // 모든 노드의 테두리를 시작할 때 키기
        if (nodeOutline != null)
        {
            nodeOutline.enabled = true;
            nodeOutline.OutlineColor = defaultColor; // 초기화
        }

        if (randomizeStartPosition && orbitCenter != null)
        {
            float randomAngle = Random.Range(0f, 360f);
            transform.RotateAround(orbitCenter.position, orbitAxis, randomAngle);
        }
    }

    private void Update()
    {
        // TODO: (테스트용) 체크박스 상태에 따라 실시간으로 색상 변경 확인
        SetClearState(isClearedStage);
        
        UpdateScale(Time.deltaTime);
        UpdateMovement(Time.deltaTime, StageManager.Instance.isMovementPaused);
    }

    // 클리어 여부에 따라 색상만 변경하는 함수
    public void SetClearState(bool isCleared)
    {
        if (nodeOutline == null) return;

        // TODO: 테두리 항상 키기 or 클리어한 것만 표시하기에 따라 변경
        nodeOutline.enabled = true;
        
        nodeOutline.OutlineColor = isCleared ? clearedColor : defaultColor;
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
        if (StageManager.Instance != null && StageManager.Instance.IsStagePauseMenuOpen) return;
        if (StageManager.Instance != null && !StageManager.Instance.CanInteractWithStagePlanets()) return;
        // 줌인 상태일 때는 호버 이벤트 무시
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = true; 
        _targetScale = _originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.IsStagePauseMenuOpen)
        {
            _isHovered = false;
            _targetScale = _originalScale;
            return;
        }
        if (StageManager.Instance != null && !StageManager.Instance.CanInteractWithStagePlanets()) return;
        // 줌인 상태일 때는 호버 이벤트 무시
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.IsStagePauseMenuOpen) return;
        if (StageManager.Instance != null && !StageManager.Instance.CanInteractWithStagePlanets()) return;
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageClicked(this);
        }
    }
}