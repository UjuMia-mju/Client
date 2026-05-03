using UnityEngine;

public class Ore : ResourceObject
{
    // Inspector에서 프리팹 등록
    [SerializeField] private GameObject orePrefab;

    [Tooltip("이 광석에서 총 몇 번 아이템이 떨어진 뒤 사라질지")]
    [SerializeField] private int maxDrops = 1;
    public override int MaxDrops => maxDrops;

    private const float ORE_THROW_HEIGHT = 3.5f;
    private const float ORE_THROW_FORCE = 150f;

    /// <summary>도구가 1회 타격했을 때 호출. 카운트/드롭/파괴는 모두 호스트가 결정.</summary>
    public override void OnHit()
    {
        Debug.Log($"[Ore] OnHit. id={resourceId}");

        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
        {
            // 호스트: 서버 매니저로 직접 전달
            ResourceServerManager.Instance.OnReceiveHit(resourceId);
        }
        else
        {
            // 피어: 호스트에게 타격 신호만 송신. 결과는 호스트가 통보.
            PacketSender.Instance.SendResourceHit(resourceId);
        }
    }

    /// <summary>기존 Pickaxe 호환용 alias.</summary>
    public void Mine() => OnHit();

    /// <summary>호스트 권위 측에서 아이템을 실제로 떨어뜨리고 피어에게 브로드캐스트.</summary>
    public override void SpawnDropAndBroadcast()
    {
        Vector3 spawnPos = transform.position + transform.up * ORE_THROW_HEIGHT;
        GameObject ore = Instantiate(orePrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = ore.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce((transform.up + transform.forward) * ORE_THROW_FORCE);

        Items itemComp = ore.GetComponent<Items>();
        if (itemComp != null)
        {
            // ⚠️ Ore가 같은 프레임에 파괴될 수 있으므로 코루틴 소유자를
            //     영속 객체(ItemManager)로 위임. 그렇지 않으면 yield 이후가 실행 안 됨.
            ItemManager.Instance.StartCoroutine(BroadcastSpawnNextFrame(itemComp, spawnPos, ore.transform.rotation));
        }
    }

    private static System.Collections.IEnumerator BroadcastSpawnNextFrame(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null; // Items.Start()의 RegisterItem() 완료 대기
        if (itemComp == null) yield break;

        PacketSender.Instance.SendObjectSpawn(itemComp, pos, rot);
        Debug.Log($"[Ore] SendObjectSpawn: itemId={itemComp.itemId}, key={itemComp.itemStringKey}");
    }
}
