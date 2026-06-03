using System.Collections.Generic;
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

    const string DecorSpinRootName = "SpinRoot";

    Transform _spinTarget;

    float _orbitRadius;
    Vector3 _orbitDirection;

    [Header("Focus / Zoom")]
    [Tooltip("줌인 시 카메라 거리 = bounds 크기 × 이 값 (최소 focusZoomDistanceMin)")]
    [SerializeField] private float focusZoomBoundsMultiplier = 2.4f;
    [SerializeField] private float focusZoomDistanceMin = 8f;

    public void Init()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;

        SetupVisualGrouping();
        AlignVisualCenterToOrbitPivot();
        DisableChildRigidbodyInterpolation();
        BindParticleSimulationLocal();

        if (nodeOutline != null)
        {
            nodeOutline.enabled = true;
            nodeOutline.OutlineColor = defaultColor;
        }

        if (orbitCenter == null)
        {
            Debug.LogWarning(
                $"[StageNode] {name}: orbitCenter가 비어 있습니다. " +
                "StageSelect 씬에서 OrbitCenter Transform을 Inspector에 연결하세요.");
        }
        else
        {
            CaptureOrbitState();

            if (randomizeStartPosition)
            {
                float randomAngle = Random.Range(0f, 360f);
                ApplyOrbitOffset(randomAngle);
            }
        }
    }

    void SetupVisualGrouping()
    {
        if (GetComponent<MeshRenderer>() != null)
        {
            // 1-1 / 1-4: FBX 메시가 루트에 있음 → 장식만 SpinRoot, 루트 pivot = 궤도
            EnsureDecorSpinRoot();
            _spinTarget = transform.Find(DecorSpinRootName);
            return;
        }

        // 1-2 / 1-3: 공전은 루트 position만. 자전은 루트 회전(자식은 hierarchy로 따라옴).
        _spinTarget = transform;
    }

    /// <summary>
    /// 1-2 / 1-3: 빈 래퍼 루트 pivot과 Planet·Meadow 등 렌더 중심이 어긋나면
    /// 자식 world position이 궤도선과 다른 원을 그립니다. 자식 local은 그대로 두고
    /// world 위치만 한 번 보정해 pivot = 비주얼 중심으로 맞춥니다.
    /// </summary>
    void AlignVisualCenterToOrbitPivot()
    {
        if (GetComponent<MeshRenderer>() != null)
            return;

        if (!TryGetFocusBounds(out Bounds bounds))
            return;

        Vector3 shift = transform.position - bounds.center;
        if (shift.sqrMagnitude < 1e-8f)
            return;

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).position += shift;
    }

    /// <summary>
    /// 부모 transform 이동 + 자식 Rigidbody Interpolate 조합은 Planet이 루트와 따로 움직이는 것처럼 보일 수 있음.
    /// </summary>
    void DisableChildRigidbodyInterpolation()
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
            rb.interpolation = RigidbodyInterpolation.None;
    }

    void EnsureDecorSpinRoot()
    {
        Transform spinRoot = transform.Find(DecorSpinRootName);
        if (spinRoot == null)
        {
            var go = new GameObject(DecorSpinRootName);
            spinRoot = go.transform;
            spinRoot.SetParent(transform, false);
            spinRoot.localPosition = Vector3.zero;
            spinRoot.localRotation = Quaternion.identity;
            spinRoot.localScale = Vector3.one;
        }

        var toMove = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == spinRoot) continue;
            toMove.Add(child);
        }

        foreach (Transform child in toMove)
            child.SetParent(spinRoot, worldPositionStays: true);
    }

    public Vector3 GetOrbitPivotWorldPosition()
    {
        return transform.position;
    }

    public Vector3 GetFocusWorldPosition()
    {
        if (TryGetFocusBounds(out Bounds bounds))
            return bounds.center;
        return transform.position;
    }

    public float GetFocusZoomDistance(float fallbackDistance)
    {
        if (!TryGetFocusBounds(out Bounds bounds))
            return fallbackDistance;

        float size = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        return Mathf.Max(focusZoomDistanceMin, size * focusZoomBoundsMultiplier);
    }

    void CaptureOrbitState()
    {
        if (orbitCenter == null) return;

        Vector3 offset = transform.position - orbitCenter.position;
        _orbitRadius = offset.magnitude;
        _orbitDirection = _orbitRadius > 0.0001f
            ? offset / _orbitRadius
            : Vector3.right;
    }

    static bool IncludeRendererForFocus(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled) return false;
        if (renderer.CompareTag(Define.Tag.NO_OUTLINE)) return false;
        return true;
    }

    bool TryGetFocusBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasAny = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!IncludeRendererForFocus(renderer)) continue;

            if (!hasAny)
            {
                bounds = renderer.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasAny;
    }

    void ApplyOrbitOffset(float angleDegrees)
    {
        if (orbitCenter == null) return;

        if (_orbitRadius <= 0.0001f)
            CaptureOrbitState();

        if (_orbitRadius <= 0.0001f) return;

        _orbitDirection = Quaternion.AngleAxis(angleDegrees, orbitAxis) * _orbitDirection;
        transform.position = orbitCenter.position + _orbitDirection * _orbitRadius;
    }

    void BindParticleSimulationLocal()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null) continue;
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    private void Update()
    {
        SetClearState(isClearedStage);
    }

    public void SetClearState(bool isCleared)
    {
        if (nodeOutline == null) return;

        nodeOutline.enabled = true;
        nodeOutline.OutlineColor = isCleared ? clearedColor : defaultColor;
    }

    public void UpdateMovement(float deltaTime, bool isGlobalPaused)
    {
        if (_isHovered) return;

        // 공전: 루트 위치만 (1-2 / 1-3 부모 오브젝트)
        if (!isGlobalPaused && orbitCenter != null)
            ApplyOrbitOffset(orbitSpeed * deltaTime);

        // 자전: 1-2/1-3은 루트, 1-1/1-4는 SpinRoot(장식만)
        if (_spinTarget != null && spinSpeed > 0f)
            _spinTarget.Rotate(spinAxis, spinSpeed * deltaTime, Space.Self);
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
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = true; 
        _targetScale = _originalScale * hoverScaleMultiplier;

        SoundManager.Instance.PlaySFX("Hover");
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
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;
        
        _isHovered = false; 
        _targetScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StageManager.Instance != null && StageManager.Instance.IsStagePauseMenuOpen) return;
        if (StageManager.Instance != null && !StageManager.Instance.CanInteractWithStagePlanets()) return;
        if (StageManager.Instance != null && StageManager.Instance.isMovementPaused) return;

        SoundManager.Instance.PlaySFX("Click2");
        
        _isHovered = false; 
        _targetScale = _originalScale;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageClicked(this);
        }
    }
}
