using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OrbitLineRenderer : MonoBehaviour
{
    [Header("Orbit References")]
    [SerializeField] private Transform orbitCenter; // 궤도의 중심점
    [SerializeField] private Vector3 orbitAxis = Vector3.up; // 회전 축 (StageNode와 동일하게 맞춤)

    [Header("Line Settings")]
    [SerializeField] private int segments = 60; // 선을 구성하는 점의 개수 (높을수록 부드러운 원)
    [SerializeField] private float lineWidth = 0.05f; // 선의 두께

    private LineRenderer _lineRenderer;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        
        // LineRenderer 기본 세팅
        _lineRenderer.positionCount = segments + 1;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        DrawOrbit();
    }

    private void DrawOrbit()
    {
        if (orbitCenter == null) return;

        // 중심점에서 현재 구체(Sphere)까지의 방향과 거리를 시작 벡터로 설정
        Vector3 startDirection = transform.position - orbitCenter.position;

        for (int i = 0; i <= segments; i++)
        {
            // 0도부터 360도까지 segments 개수만큼 쪼개서 각도 계산
            float currentAngle = ((float)i / segments) * 360f;
            
            // 지정한 축(orbitAxis)을 기준으로 회전하는 쿼터니언 생성
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, orbitAxis);
            
            // 중심점 위치에 회전된 벡터를 더해 궤도 위의 3D 좌표를 구함
            Vector3 point = orbitCenter.position + (rotation * startDirection);
            
            _lineRenderer.SetPosition(i, point);
        }
    }
}