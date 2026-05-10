using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SporeCloudDebuffZone : MonoBehaviour
{
    [Header("Cloud Lifetime")]
    [SerializeField] private float cloudLifetime = 6f;

    [Header("Cloud Area")]
    [SerializeField] private float cloudRadius = 3.2f;

    [Header("Debuff")]
    [SerializeField] private float scrambleDuration = 2.5f;
    [SerializeField] private float reapplyInterval = 0.5f;

    private readonly HashSet<Player> _playersInZone = new HashSet<Player>();
    private SphereCollider _trigger;
    private float _nextReapplyTime;

    private void Awake()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = cloudRadius;
    }

    private void OnEnable()
    {
        _nextReapplyTime = Time.time;
        Destroy(gameObject, cloudLifetime);
    }

    private void Update()
    {
        if (Time.time < _nextReapplyTime) return;
        _nextReapplyTime = Time.time + reapplyInterval;

        if (_playersInZone.Count == 0) return;

        _playersInZone.RemoveWhere(p => p == null);
        foreach (Player player in _playersInZone)
            player.ApplyMoveInputScramble(scrambleDuration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        _playersInZone.Add(player);
        player.ApplyMoveInputScramble(scrambleDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        _playersInZone.Remove(player);
    }
}
