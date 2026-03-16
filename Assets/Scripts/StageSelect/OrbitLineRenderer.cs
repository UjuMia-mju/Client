using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class OrbitLineRenderer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")] 
    public StageNode targetNode;
    public Transform orbitCenter;

    [Header("Text Settings")] 
    public TextMeshPro orbitText;
    public Color textHoverColor = Color.yellow;

    [Header("Line Settings")] 
    public int segments = 60;
    public float lineWidth = 0.05f;

    [Header("Hover Settings")] 
    public Color hoverColor = Color.yellow;
    public float hitBoxMultiplier = 5f;

    private Gradient _originalGradient;
    private LineRenderer _lineRenderer;
    private MeshCollider _meshCollider;
    private Color _originalTextColor;

    private void Start()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        _lineRenderer = GetComponent<LineRenderer>();
        _originalGradient = _lineRenderer.colorGradient;

        // NOTE: 이 부분이 있어야 마우스가 나갔을 때 텍스트 색상이 원래대로 잘 돌아와!
        if (orbitText != null)
        {
            _originalTextColor = orbitText.color;
        }

        DrawOrbit();
        GenerateMeshCollider();
    }

    private void Update()
    {
        if (orbitText != null && Camera.main != null)
        {
            orbitText.transform.rotation = Quaternion.LookRotation(orbitText.transform.position - Camera.main.transform.position);
        }
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

    private void GenerateMeshCollider()
    {
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
        SetLineColor(hoverColor);
        if (orbitText != null) orbitText.color = textHoverColor;
        if (targetNode != null) targetNode.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _lineRenderer.colorGradient = _originalGradient;
        if (orbitText != null) orbitText.color = _originalTextColor;
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