using UnityEngine;

public class TreeResource : ResourceObject
{
    // Inspector에서 프리팹 등록
    [SerializeField] private GameObject logPrefab;
    [Header("SFX")]
    [SerializeField] private string hitSfxName = "TreeHit";
    [SerializeField, Range(0f, 1f)] private float hitSfxVolumeScale = 0.9f;

    [Tooltip("이 나무에서 총 몇 번 아이템이 떨어진 뒤 사라질지")]
    [SerializeField] private int maxDrops = 1;
    public override int MaxDrops => maxDrops;

    private const float ORE_THROW_HEIGHT = 3.5f;
    private const float ORE_THROW_FORCE = 150f;

    /// <summary>도구가 1회 타격했을 때 호출. 카운트/드롭/파괴는 모두 호스트가 결정.</summary>
    public override void OnHit()
    {
        Debug.Log($"[Tree] OnHit. id={resourceId}");
        SoundManager.Instance?.PlaySFXAt(
            hitSfxName,
            transform.position,
            volumeScale: hitSfxVolumeScale,
            minPitch: 0.95f,
            maxPitch: 1.05f,
            minDistance: 2f,
            maxDistance: 16f);

        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
        {
            ResourceServerManager.Instance.OnReceiveHit(resourceId);
        }
        else
        {
            PacketSender.Instance.SendResourceHit(resourceId);
        }
    }

    /// <summary>기존 Axe 호환용 alias.</summary>
    public void Logging() => OnHit();

    /// <summary>호스트 권위 측에서 아이템을 실제로 떨어뜨리고 피어에게 브로드캐스트.</summary>
    public override void SpawnDropAndBroadcast()
    {
        Vector3 up = GetPlanetOutwardUp();
        Vector3 spawnPos = transform.position + up * ORE_THROW_HEIGHT;
        GameObject wood = Instantiate(logPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = wood.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce((up + transform.forward) * ORE_THROW_FORCE);

        Items itemComp = wood.GetComponent<Items>();
        if (itemComp != null)
        {
            // ⚠️ Tree가 같은 프레임에 파괴될 수 있으므로 ItemManager 코루틴으로 위임.
            ItemManager.Instance.StartCoroutine(BroadcastSpawnNextFrame(itemComp, spawnPos, wood.transform.rotation));
        }
    }

    private static System.Collections.IEnumerator BroadcastSpawnNextFrame(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null;
        if (itemComp == null) yield break;

        Vector3 currentPos = itemComp.transform.position;
        Quaternion currentRot = itemComp.transform.rotation;

        PacketSender.Instance.SendObjectSpawn(itemComp, currentPos, currentRot);
        Debug.Log($"[Tree] SendObjectSpawn: itemId={itemComp.itemId}, key={itemComp.itemStringKey}, pos={currentPos}");
    }
}
