using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PoisonLakeZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageIntervalSeconds = 1.2f;

    [Header("Visual (Optional)")]
    [SerializeField] private Renderer lakeRenderer;
    [SerializeField] private bool applyVisualOnEnable = true;
    [SerializeField] private Color lakeColor = new Color(0.56f, 0.22f, 0.72f, 0.65f);
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Shape/Depth Tuning (Optional)")]
    [SerializeField] private bool applyShapeOnEnable = false;
    [SerializeField] private Vector3 boxSize = new Vector3(8f, 1.5f, 8f);
    [SerializeField] private float sphereRadius = 4f;
    [SerializeField] private float capsuleRadius = 3f;
    [SerializeField] private float capsuleHeight = 3f;

    private readonly HashSet<PlayerStat> _playersInside = new HashSet<PlayerStat>();
    private readonly Dictionary<PlayerStat, float> _nextTickTimeByPlayer = new Dictionary<PlayerStat, float>();
    private Collider _trigger;
    private Material _runtimeMat;

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        if (applyVisualOnEnable)
            ApplyVisual();

        if (applyShapeOnEnable)
            ApplyShape();
    }

    private void Update()
    {
        if (_playersInside.Count == 0)
            return;

        float now = Time.time;
        _playersInside.RemoveWhere(p => p == null);

        foreach (PlayerStat stat in _playersInside)
        {
            if (!_nextTickTimeByPlayer.TryGetValue(stat, out float nextTime))
                continue;

            if (now < nextTime)
                continue;

            ApplyDamage(stat);
            _nextTickTimeByPlayer[stat] = now + damageIntervalSeconds;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayerStat(other, out PlayerStat stat))
            return;

        _playersInside.Add(stat);

        // 들어온 즉시 1회 피해 (재진입 시에도 즉시 다시 적용)
        ApplyDamage(stat);
        _nextTickTimeByPlayer[stat] = Time.time + damageIntervalSeconds;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayerStat(other, out PlayerStat stat))
            return;

        _playersInside.Remove(stat);
        _nextTickTimeByPlayer.Remove(stat);
    }

    private bool TryGetPlayerStat(Collider other, out PlayerStat stat)
    {
        stat = null;
        if (other == null || !other.CompareTag(Define.Tag.PLAYER))
            return false;

        stat = other.GetComponentInParent<PlayerStat>();
        return stat != null;
    }

    private void ApplyDamage(PlayerStat stat)
    {
        if (stat == null || damagePerTick <= 0)
            return;

        stat.DecreaseHp(damagePerTick);
    }

    [ContextMenu("Apply Visual")]
    public void ApplyVisual()
    {
        if (lakeRenderer == null)
            return;

        _runtimeMat = lakeRenderer.material;
        if (_runtimeMat == null)
            return;

        if (_runtimeMat.HasProperty(colorPropertyName))
            _runtimeMat.SetColor(colorPropertyName, lakeColor);
        else if (_runtimeMat.HasProperty("_Color"))
            _runtimeMat.SetColor("_Color", lakeColor);
    }

    [ContextMenu("Apply Shape")]
    public void ApplyShape()
    {
        if (_trigger == null)
            _trigger = GetComponent<Collider>();

        if (_trigger is BoxCollider box)
        {
            box.size = boxSize;
            return;
        }

        if (_trigger is SphereCollider sphere)
        {
            sphere.radius = sphereRadius;
            return;
        }

        if (_trigger is CapsuleCollider capsule)
        {
            capsule.radius = capsuleRadius;
            capsule.height = Mathf.Max(capsuleHeight, capsuleRadius * 2f);
        }
    }
}
