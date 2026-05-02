using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 호스트 권위 자원 매니저.
/// 피어로부터 받은 C_RESOURCE_HIT을 누적해 임계점에서 아이템 드롭/자원 소멸을 결정하고
/// S_RESOURCE_DESTROY로 피어에게 결과를 전파합니다.
/// 호스트 자신의 채굴도 동일 경로(OnReceiveHit)로 처리하여 단일 진실의 원천을 유지합니다.
/// </summary>
public class ResourceServerManager : MonoBehaviorSingleton<ResourceServerManager>
{
    /// <summary>resourceId → 누적 타격 횟수 (호스트 전용).</summary>
    private readonly Dictionary<int, int> _hitCounts = new Dictionary<int, int>();
    /// <summary>resourceId → 누적 드롭 횟수 (호스트 전용).</summary>
    private readonly Dictionary<int, int> _dropCounts = new Dictionary<int, int>();

    private const int HITS_PER_DROP = 3; // N회 타격마다 1회 드롭

    private void Start()
    {
        if (PeerPacketHandler.Instance != null)
            PeerPacketHandler.Instance.OnPeerResourceHitEvent += OnPeerResourceHit;
    }

    private void OnDestroy()
    {
        if (PeerPacketHandler.Instance != null)
            PeerPacketHandler.Instance.OnPeerResourceHitEvent -= OnPeerResourceHit;
    }

    private void OnPeerResourceHit(int peerId, Protocol.C_RESOURCE_HIT packet)
    {
        OnReceiveHit(packet.ResourceId);
    }

    /// <summary>
    /// 자원 1회 타격 처리. 호스트 권위.
    /// 호스트 본인의 채굴(Pickaxe → Ore.OnHit)도 이 메서드를 직접 호출.
    /// </summary>
    public void OnReceiveHit(int resourceId)
    {
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
        {
            Debug.LogWarning("[ResourceServerManager] 비호스트가 OnReceiveHit를 호출했습니다.");
            return;
        }

        ResourceObject resource = ResourceManager.Instance?.GetResource(resourceId);
        if (resource == null)
        {
            Debug.LogWarning($"[ResourceServerManager] 자원 없음: id={resourceId}");
            return;
        }

        // 1. 타격 누적
        _hitCounts.TryGetValue(resourceId, out int hits);
        hits++;
        _hitCounts[resourceId] = hits;
        Debug.Log($"[ResourceServerManager] 타격 누적: id={resourceId}, hits={hits}");

        if (hits < HITS_PER_DROP) return;

        // 2. 드롭 트리거
        _hitCounts[resourceId] = 0; // 다음 드롭을 위한 리셋
        resource.SpawnDropAndBroadcast(); // 호스트 측 Instantiate + AddForce + S_OBJECT_SPAWN 브로드캐스트

        _dropCounts.TryGetValue(resourceId, out int drops);
        drops++;
        _dropCounts[resourceId] = drops;
        Debug.Log($"[ResourceServerManager] 드롭: id={resourceId}, dropsSoFar={drops}/{resource.MaxDrops}");

        // 3. 드롭 한도 도달 시 자원 소멸
        if (drops >= resource.MaxDrops)
        {
            DestroyResource(resourceId);
        }
    }

    private void DestroyResource(int resourceId)
    {
        _hitCounts.Remove(resourceId);
        _dropCounts.Remove(resourceId);

        // 호스트 로컬 파괴
        ResourceManager.Instance.DestroyResourceFromNetwork(resourceId);
        // 피어 브로드캐스트
        PacketSender.Instance.BroadcastResourceDestroy(resourceId);
        Debug.Log($"[ResourceServerManager] 자원 소진 → 파괴/브로드캐스트: id={resourceId}");
    }
}