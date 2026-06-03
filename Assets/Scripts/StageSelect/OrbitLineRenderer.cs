using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(LineRenderer))]
public class OrbitLineRenderer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")] 
    public StageNode targetNode;
    public Transform orbitCenter;

    [Header("Line Settings")] 
    public int segments = 60;
    public float lineWidth = 0.05f;

    [Header("Hover Settings")] 
    public Color hoverColor = Color.yellow;
    public float hitBoxMultiplier = 5f;

    private Gradient _originalGradient;
    private LineRenderer _lineRenderer;
    private MeshCollider _meshCollider;

    private void Start()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_lineRenderer != null) return;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        _lineRenderer = GetComponent<LineRenderer>();
        _originalGradient = _lineRenderer.colorGradient;
    }

    /// <summary>StageNode.Init() 이후 행성 위치에 맞춰 궤도선·히트박스를 다시 그립니다.</summary>
    public void RedrawOrbit()
    {
        EnsureInitialized();
        DrawOrbit();
        GenerateMeshCollider();
    }

    private void DrawOrbit()
    {
        if (_lineRenderer == null || orbitCenter == null || targetNode == null) return;

        _lineRenderer.positionCount = segments;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.loop = true;

        Vector3 startDirection = targetNode.GetOrbitPivotWorldPosition() - orbitCenter.position;

        for (int i = 0; i < segments; i++)
        {
            float currentAngle = ((float)i / segments) * 360f;
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, targetNode.orbitAxis);
            Vector3 point = orbitCenter.position + (rotation * startDirection);

            _lineRenderer.SetPosition(i, point);
        }
    }

    private void GenerateMeshCollider()
    {
        if (_lineRenderer == null || Camera.main == null) return;

        _lineRenderer.startWidth = lineWidth * hitBoxMultiplier;
        _lineRenderer.endWidth = lineWidth * hitBoxMultiplier;

        Mesh fatMesh = new Mesh();
        _lineRenderer.BakeMesh(fatMesh, Camera.main, true);

        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        _meshCollider = gameObject.GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = gameObject.AddComponent<MeshCollider>();

        _meshCollider.sharedMesh = fatMesh;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanUseOrbitLineInteraction()) return;

        SetLineColor(hoverColor);
        if (targetNode != null) targetNode.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_lineRenderer != null)
            _lineRenderer.colorGradient = _originalGradient;
        if (targetNode != null && CanUseOrbitLineInteraction())
            targetNode.OnPointerExit(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanUseOrbitLineInteraction()) return;
        if (targetNode != null) targetNode.OnPointerClick(eventData);
    }

    /// <summary>StageNode와 동일 정책: 게스트(비호스트)는 궤도 호버/클릭 불가.</summary>
    static bool CanUseOrbitLineInteraction()
    {
        if (StageManager.Instance == null) return true;
        if (StageManager.Instance.IsStagePauseMenuOpen) return false;
        if (!StageManager.Instance.CanInteractWithStagePlanets()) return false;
        if (StageManager.Instance.isMovementPaused) return false;
        return true;
    }

    private void SetLineColor(Color color)
    {
        if (_lineRenderer == null) return;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        _lineRenderer.colorGradient = gradient;
    }
}