using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PoisonMushroom : MonoBehaviour
{
    private const string SyncCommandPrefix = "__PM_EXPLODE__";
    private static readonly Dictionary<int, PoisonMushroom> SyncRegistry = new Dictionary<int, PoisonMushroom>();

    [Header("Trigger")]
    [SerializeField] private float triggerRadius = 2.2f;

    [Header("Explosion Result")]
    [SerializeField] private GameObject sporeCloudPrefab;
    [SerializeField] private Vector3 cloudSpawnOffset = Vector3.up * 0.2f;
    [SerializeField] private Vector3 cloudSpawnEulerOffset = Vector3.zero;
    [SerializeField] private GameObject explodeEffectPrefab;
    [SerializeField] private bool destroyOnExplode = true;
    
    [Header("Explosion SFX")]
    [SerializeField] private string explodeSfxName = "PoisonMushroomExplode";
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.05f;

    [Header("Network Sync")]
    [SerializeField] private bool syncExplosionAcrossNetwork = true;
    [SerializeField, Tooltip("동일 ID를 찾지 못했을 때 위치로 매칭할 최대 거리")]
    private float fallbackMatchRadius = 2.5f;

    private SphereCollider _trigger;
    private bool _exploded;
    private int _syncId;

    private void Awake()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = triggerRadius;

        _syncId = ComputeStableSyncId();
        SyncRegistry[_syncId] = this;
    }

    private void OnDestroy()
    {
        if (SyncRegistry.TryGetValue(_syncId, out PoisonMushroom current) && current == this)
            SyncRegistry.Remove(_syncId);
    }

    private void OnEnable()
    {
        MushroomExplosionSyncBus.OnExplodePayload += OnNetworkExplodePayload;
    }

    private void OnDisable()
    {
        MushroomExplosionSyncBus.OnExplodePayload -= OnNetworkExplodePayload;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exploded) return;
        if (other == null) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        if (!syncExplosionAcrossNetwork || ConnectManager.Instance == null)
        {
            Explode();
            return;
        }

        if (ConnectManager.Instance.isHost)
        {
            Explode();
            byte[] payload = BuildSyncPayload(_syncId, transform.position);
            MushroomExplosionSyncBus.BroadcastExplodeFromHost(payload);
            return;
        }

        MushroomExplosionSyncBus.SendExplodeRequest(BuildSyncPayload(_syncId, transform.position));
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
        if (string.IsNullOrEmpty(explodeSfxName))
            return;

        SoundManager.Instance?.PlaySFXAt(
            explodeSfxName,
            position,
            volumeScale: sfxVolume,
            minPitch: minPitch,
            maxPitch: maxPitch,
            minDistance: 2f,
            maxDistance: 18f);
    }

    public static bool TryApplyNetworkExplodeCommand(string message)
    {
        if (string.IsNullOrEmpty(message) || !message.StartsWith(SyncCommandPrefix))
            return false;

        string[] tokens = message.Split(':');
        if (tokens.Length != 5)
            return true;

        if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int syncId))
            return true;

        if (!float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
            return true;
        if (!float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
            return true;
        if (!float.TryParse(tokens[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float pz))
            return true;

        Vector3 pos = new Vector3(px, py, pz);
        if (SyncRegistry.TryGetValue(syncId, out PoisonMushroom target) && target != null)
        {
            target.Explode();
            return true;
        }

        PoisonMushroom nearest = FindNearest(pos);
        nearest?.Explode();
        return true;
    }

    private static PoisonMushroom FindNearest(Vector3 position)
    {
        PoisonMushroom best = null;
        float bestSqr = float.MaxValue;

        foreach (var kv in SyncRegistry)
        {
            PoisonMushroom mushroom = kv.Value;
            if (mushroom == null || mushroom._exploded)
                continue;

            float sqr = (mushroom.transform.position - position).sqrMagnitude;
            float max = mushroom.fallbackMatchRadius * mushroom.fallbackMatchRadius;
            if (sqr <= max && sqr < bestSqr)
            {
                best = mushroom;
                bestSqr = sqr;
            }
        }

        return best;
    }

    private void OnNetworkExplodePayload(byte[] payload)
    {
        TryApplyNetworkExplodePayload(payload);
    }

    public static bool TryApplyNetworkExplodePayload(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
            return false;

        string message = Encoding.UTF8.GetString(payload);
        return TryApplyNetworkExplodeCommand(message);
    }

    private static string BuildSyncCommand(int syncId, Vector3 position)
    {
        return SyncCommandPrefix + ":" +
               syncId.ToString(CultureInfo.InvariantCulture) + ":" +
               position.x.ToString("R", CultureInfo.InvariantCulture) + ":" +
               position.y.ToString("R", CultureInfo.InvariantCulture) + ":" +
               position.z.ToString("R", CultureInfo.InvariantCulture);
    }

    private static byte[] BuildSyncPayload(int syncId, Vector3 position)
    {
        return Encoding.UTF8.GetBytes(BuildSyncCommand(syncId, position));
    }

    private int ComputeStableSyncId()
    {
        string path = BuildHierarchyPath(transform);
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < path.Length; i++)
                hash = hash * 31 + path[i];
            return hash;
        }
    }

    private static string BuildHierarchyPath(Transform t)
    {
        List<string> parts = new List<string>(16);
        Transform cur = t;
        while (cur != null)
        {
            parts.Add($"{cur.name}[{cur.GetSiblingIndex()}]");
            cur = cur.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
