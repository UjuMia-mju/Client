using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlanetScatterTool : MonoBehaviour
{
    [Header("Target")]
    public Transform planetCenter;
    [Tooltip("Meadow 등 메시 행성일 때만 지정. 구체 Planet은 비워두면 Transform 스케일로 표면을 계산합니다.")]
    public Collider planetCollider;
    public Transform parentRoot;

    [Header("Prefabs")]
    public List<GameObject> prefabs = new List<GameObject>();

    [Header("Scatter")]
    public int count = 500;
    public float castHeight = 200f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public LayerMask hitMask = ~0;

    [Header("Filter")]
    [Range(0f, 1f)] public float minDotToUp = 0.35f; // 급경사 제외
    public float normalOffset = 0f; // 파묻힘/뜸 보정
    [Tooltip("0이면 간격 제한 없음. 이미 배치된 잔디와의 최소 거리(월드 단위).")]
    public float minSpacing = 0f;
    [Tooltip("한 개 배치당 최대 재시도 횟수 (간격 때문에 실패 시).")]
    public int maxAttemptsPerInstance = 24;

    [Header("Random Yaw")]
    public bool randomYaw = true;

    [ContextMenu("Scatter")]
    public void Scatter()
    {
        if (!planetCenter || !parentRoot || prefabs.Count == 0) return;

        var placedPoints = new List<Vector3>(count);
        float spacingSqr = minSpacing > 0f ? minSpacing * minSpacing : 0f;
        int placed = 0;
        int maxAttempts = Mathf.Max(count * Mathf.Max(1, maxAttemptsPerInstance), count);

        for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
        {
            Vector3 dir = Random.onUnitSphere;
            if (!TrySampleSurface(dir, out RaycastHit hit))
                continue;

            // 행성 바깥 방향(중심 -> hit점)
            Vector3 outward = (hit.point - planetCenter.position).normalized;

            // 표면 노말과 바깥 방향이 너무 다르면 스킵 (벽면/언더사이드 방지)
            float d = Vector3.Dot(hit.normal.normalized, outward);
            if (d < minDotToUp) continue;

            Vector3 spawnPoint = hit.point + hit.normal * normalOffset;
            if (spacingSqr > 0f)
            {
                bool tooClose = false;
                for (int i = 0; i < placedPoints.Count; i++)
                {
                    if ((placedPoints[i] - spawnPoint).sqrMagnitude < spacingSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (!prefab) continue;

            GameObject go = Instantiate(prefab, parentRoot);
            go.transform.position = spawnPoint;
            placedPoints.Add(spawnPoint);
            placed++;

            // 표면 노말에 수직 정렬
            Quaternion align = Quaternion.FromToRotation(Vector3.up, hit.normal);
            go.transform.rotation = align;

            // yaw 랜덤
            if (randomYaw)
                go.transform.Rotate(hit.normal, Random.Range(0f, 360f), Space.World);

            float s = Random.Range(minScale, maxScale);
            go.transform.localScale *= s;
        }
    }

    [ContextMenu("Clear Children")]
    public void ClearChildren()
    {
#if UNITY_EDITOR
        while (parentRoot.childCount > 0)
            DestroyImmediate(parentRoot.GetChild(0).gameObject);
#else
        foreach (Transform c in parentRoot) Destroy(c.gameObject);
#endif
    }

    bool TrySampleSurface(Vector3 outwardDir, out RaycastHit hit)
    {
        hit = default;
        Vector3 dir = outwardDir.normalized;
        Vector3 origin = planetCenter.position + dir * castHeight;
        Vector3 rayDir = -dir;

        if (planetCollider is MeshCollider)
        {
            if (!Physics.Raycast(origin, rayDir, out hit, castHeight * 2f, hitMask))
                return false;
            return hit.collider == planetCollider;
        }

        float radius = GetPlanetWorldRadius();
        return RaycastSphere(planetCenter.position, radius, origin, rayDir, out hit);
    }

    float GetPlanetWorldRadius()
    {
        if (planetCollider is SphereCollider sphereCollider)
        {
            Vector3 scale = sphereCollider.transform.lossyScale;
            float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return sphereCollider.radius * maxScale;
        }

        // Sphere1M 등 단위 구(지름 1) 행성 메시 기준
        Vector3 planetScale = planetCenter.lossyScale;
        return Mathf.Max(planetScale.x, Mathf.Max(planetScale.y, planetScale.z)) * 0.5f;
    }

    static bool RaycastSphere(Vector3 center, float radius, Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        hit = default;
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, direction);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float discriminant = b * b - c;
        if (discriminant < 0f)
            return false;

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t = -b - sqrtDisc;
        if (t < 0f)
            t = -b + sqrtDisc;
        if (t < 0f)
            return false;

        hit.point = origin + direction * t;
        hit.normal = (hit.point - center).normalized;
        hit.distance = t;
        return true;
    }
}