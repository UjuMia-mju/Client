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
    
    [Tooltip("선 클릭 판정(히트박스) 두께 배수")]
    public float hitBoxMultiplier = 5f; 
    
    private Gradient _originalGradient;
    private LineRenderer _lineRenderer;
    private MeshCollider _meshCollider;

    private void Start()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        _lineRenderer = GetComponent<LineRenderer>();
        _originalGradient = _lineRenderer.colorGradient;

        DrawOrbit();
        GenerateMeshCollider(); // 여기서 두꺼운 히트박스를 만듦!
    }

    private void DrawOrbit()
    {
        if (orbitCenter == null || targetNode == null) return;

        _lineRenderer.positionCount = segments;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.loop = true;

        Vector3 startDirection = targetNode.transform.position - orbitCenter.position;

        for (int i = 0; i < segments; i++)
        {
            float currentAngle = ((float)i / segments) * 360f;
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, targetNode.orbitAxis);
            Vector3 point = orbitCenter.position + (rotation * startDirection);
            
            _lineRenderer.SetPosition(i, point);
        }
    }

    // =========================================================
    // 핵심 꼼수: 두꺼운 메쉬 콜라이더 굽기
    // =========================================================
    private void GenerateMeshCollider()
    {
        // 1. 선 두께를 굽기 전 임시로 엄청 두껍게 만듦
        _lineRenderer.startWidth = lineWidth * hitBoxMultiplier;
        _lineRenderer.endWidth = lineWidth * hitBoxMultiplier;

        // 2. 뚱뚱해진 상태의 선 모양대로 3D 메쉬를 구워냄
        Mesh fatMesh = new Mesh();
        _lineRenderer.BakeMesh(fatMesh, Camera.main, true);

        // 3. 눈에 보이는 선 두께는 다시 원래대로(얇게) 원상복구!
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        // 4. 구워낸 뚱뚱한 메쉬를 물리 충돌체(콜라이더)에 덮어씌움
        _meshCollider = gameObject.GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            
        _meshCollider.sharedMesh = fatMesh;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetLineColor(hoverColor);
        if (targetNode != null) targetNode.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _lineRenderer.colorGradient = _originalGradient;
        if (targetNode != null) targetNode.OnPointerExit(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetNode != null) targetNode.OnPointerClick(eventData);
    }

    private void SetLineColor(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        _lineRenderer.colorGradient = gradient;
    }
}