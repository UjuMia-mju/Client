using UnityEngine;

public class TreeResource : MonoBehaviour
{
    // Inspector에서 프리팹 등록
    [SerializeField] private GameObject logPrefab;

    private int miningCount = 0;

    private const float ORE_THROW_HEIGHT = 3.5f;
    private const float ORE_THROW_FORCE = 150f;

    public void Logging()
    {
        Debug.Log("벌목");
        miningCount++;
        if (miningCount >= 3) // 3회 이상 채굴 시 아이템 드롭
        {
            DropItem();
            miningCount = 0;
        }
    }

    private void DropItem()
    {
        Debug.Log("아이템 드랍");

        Vector3 spawnPos = this.transform.position + this.transform.up * ORE_THROW_HEIGHT;

        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
        {
            // 호스트: 직접 스폰 + 브로드캐스트
            GameObject wood = Instantiate(logPrefab, spawnPos, Quaternion.identity);
            Rigidbody rb = wood.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce((this.transform.up + this.transform.forward) * ORE_THROW_FORCE);
            Items itemComp = wood.GetComponent<Items>();
            if (itemComp != null)
                StartCoroutine(BroadcastSpawnNextFrame(itemComp, spawnPos, wood.transform.rotation));
        }
        else
        {
            // 피어: 로컬 스폰 없이 요청만 전송
            Items prefabItem = logPrefab.GetComponent<Items>();
            if (prefabItem != null)
                PacketSender.Instance.SendObjectSpawnRequest(prefabItem.itemStringKey, spawnPos, Quaternion.identity);
        }
    }

    private System.Collections.IEnumerator BroadcastSpawnNextFrame(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null; // Items.Start()의 RegisterItem() 완료 대기
        PacketSender.Instance.SendObjectSpawn(itemComp, pos, rot);
        Debug.Log($"[Tree] SendObjectSpawn: itemId={itemComp.itemId}, key={itemComp.itemStringKey}");
    }
}
