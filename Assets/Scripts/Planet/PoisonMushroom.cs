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
    
    [Header("Explosion SFX")]
    [SerializeField] private AudioClip explodeSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.05f;

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
        PlayExplodeSfx(spawnPos);

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

    private void PlayExplodeSfx(Vector3 position)
    {
        if (explodeSfx == null)
            return;

        var sfxObj = new GameObject("PoisonMushroomSfx");
        sfxObj.transform.position = position;

        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = explodeSfx;
        source.volume = sfxVolume;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 18f;
        source.Play();

        float lifeTime = explodeSfx.length / Mathf.Max(0.01f, source.pitch) + 0.05f;
        Destroy(sfxObj, lifeTime);
    }
}
