using UnityEngine;

public class TreasureResource : ResourceObject
{
    [Header("Treasure")]
    [Tooltip("이 보물이 트랩이면 보석 대신 DesertWorm이 스폰됩니다.")]
    public bool isTrap;

    [Tooltip("isTrap=false 일 때 떨어질 보석 프리팹")]
    [SerializeField] private GameObject gemPrefab;

    [Tooltip("이 보물에서 총 몇 번 아이템이 떨어진 뒤 사라질지")]
    [SerializeField] private int maxDrops = 1;
    public override int MaxDrops => maxDrops;

    [Header("Trap Spawn")]
    [Tooltip("isTrap=true일 때 DesertWorm을 보물 위치에서 얼마나 위로 띄워 스폰할지")]
    [SerializeField] private float wormSpawnUpOffset = 0f;

    private const float GEM_THROW_HEIGHT = 0f;
    private const float GEM_THROW_FORCE = 0f;

    // 도구가 1회 타격했을 때 호출. 카운트/드롭/파괴는 모두 호스트가 결정.
    public override void OnHit()
    {
        Debug.Log($"[Treasure] OnHit. id={resourceId}, isTrap={isTrap}");

        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
            ResourceServerManager.Instance.OnReceiveHit(resourceId);
        else
            PacketSender.Instance.SendResourceHit(resourceId);
    }

    // Shovel 호환용 alias.
    public void Dig() => OnHit();

    // 호스트 권위 측에서 보석을 떨어뜨리거나, 트랩이면 DesertWorm을 스폰.
    public override void SpawnDropAndBroadcast()
    {
        if (isTrap)
        {
            SpawnWormTrap();
            return;
        }

        SpawnGemDrop();
    }

    // ===== 일반 보물: 보석 드롭 =====
    private void SpawnGemDrop()
    {
        if (gemPrefab == null)
        {
            Debug.LogWarning($"[Treasure] gemPrefab 미설정. id={resourceId}");
            return;
        }

        Vector3 spawnPos = transform.position + transform.up * GEM_THROW_HEIGHT;
        GameObject gem = Instantiate(gemPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = gem.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce((transform.up + transform.forward) * GEM_THROW_FORCE);

        Items itemComp = gem.GetComponent<Items>();
        if (itemComp != null)
            ItemManager.Instance.StartCoroutine(BroadcastSpawnNextFrame(itemComp));
    }

    private static System.Collections.IEnumerator BroadcastSpawnNextFrame(Items itemComp)
    {
        yield return null;
        if (itemComp == null) yield break;

        Vector3 currentPos = itemComp.transform.position;
        Quaternion currentRot = itemComp.transform.rotation;

        PacketSender.Instance.SendObjectSpawn(itemComp, currentPos, currentRot);
        Debug.Log($"[Treasure] SendObjectSpawn(gem): itemId={itemComp.itemId}, key={itemComp.itemStringKey}");
    }

    // ===== 트랩: DesertWorm 스폰 =====
    private void SpawnWormTrap()
    {
        if (MonsterManager.Instance == null)
        {
            Debug.LogWarning("[Treasure] MonsterManager 없음 → 트랩 발동 실패");
            return;
        }

        Vector3 spawnPos = transform.position + transform.up * wormSpawnUpOffset;

        PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
        Quaternion spawnRot = planet != null
            ? Quaternion.FromToRotation(Vector3.up, (spawnPos - planet.transform.position).normalized)
            : transform.rotation;

        MonsterManager.Instance.SpawnMonster(Monsters.DesertWorm, spawnPos, spawnRot);
        Debug.Log($"[Treasure] 트랩 발동! DesertWorm 스폰. id={resourceId}");
    }
}
