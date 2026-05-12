using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PoisonMushroom : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private float triggerRadius = 2.2f;

    [Header("Explosion Result")]
    [SerializeField] private GameObject sporeCloudPrefab;
    [SerializeField] private Vector3 cloudSpawnOffset = Vector3.up * 0.2f;
    [SerializeField] private Vector3 cloudSpawnEulerOffset = Vector3.zero;
    [SerializeField] private GameObject explodeEffectPrefab;
    [SerializeField] private bool destroyOnExplode = true;

    private SphereCollider _trigger;
    private bool _exploded;

    private void Awake()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = triggerRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exploded) return;
        if (other == null) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        Vector3 spawnPos = transform.position + cloudSpawnOffset;
        Quaternion spawnRot = transform.rotation * Quaternion.Euler(cloudSpawnEulerOffset);

        if (explodeEffectPrefab != null)
            Instantiate(explodeEffectPrefab, spawnPos, spawnRot);

        if (sporeCloudPrefab != null)
            Instantiate(sporeCloudPrefab, spawnPos, spawnRot);

        if (destroyOnExplode)
        {
            Destroy(gameObject);
            return;
        }

        // 비파괴 옵션일 때는 재트리거를 막고 비주얼만 끕니다.
        _trigger.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }
}
