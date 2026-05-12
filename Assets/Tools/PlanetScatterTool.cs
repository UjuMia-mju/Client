using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlanetScatterTool : MonoBehaviour
{
    [Header("Target")]
    public Transform planetCenter;
    public Collider planetCollider;   // MeshCollider 권장
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

    [Header("Random Yaw")]
    public bool randomYaw = true;

    [ContextMenu("Scatter")]
    public void Scatter()
    {
        if (!planetCenter || !planetCollider || !parentRoot || prefabs.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            Vector3 origin = planetCenter.position + dir * castHeight;

            if (!Physics.Raycast(origin, -dir, out RaycastHit hit, castHeight * 2f, hitMask))
                continue;

            if (hit.collider != planetCollider)
                continue;

            // 행성 바깥 방향(중심 -> hit점)
            Vector3 outward = (hit.point - planetCenter.position).normalized;

            // 표면 노말과 바깥 방향이 너무 다르면 스킵 (벽면/언더사이드 방지)
            float d = Vector3.Dot(hit.normal.normalized, outward);
            if (d < minDotToUp) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (!prefab) continue;

            GameObject go = Instantiate(prefab, parentRoot);
            go.transform.position = hit.point + hit.normal * normalOffset;

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
}