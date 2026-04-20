using Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    [System.Serializable]
    public class ItemPrefabData
    {
        public string itemStringKey;
        public GameObject prefab;
    }

    [Header("Item Prefab Table")]
    [SerializeField] private List<ItemPrefabData> itemPrefabList = new List<ItemPrefabData>();

    private static int _nextItemId = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextItemId = 1;
    }

    public void RegisterItem(Items item)
    {
        item.itemId = _nextItemId++;
        if (!itemDic.ContainsKey(item.itemId))
        {
            itemDic.Add(item.itemId, item);
            Debug.Log($"✓ Registered item: {item.name} (id={item.itemId})");
        }
    }

    public void UnregisterItem(Items item)
    {
        if (itemDic.ContainsKey(item.itemId))
        {
            itemDic.Remove(item.itemId);
            Debug.Log($"✓ Unregistered item: {item.name} (id={item.itemId})");
        }
    }

    public Items GetItem(int id)
    {
        if (itemDic.TryGetValue(id, out Items item))
            return item;
        return null;
    }

    public Items GetItemByStringKey(string key)
    {
        foreach (var item in itemDic.Values)
        {
            if (item.itemStringKey == key)
                return item;
        }
        return null;
    }

    public void OverrideItemId(Items item, int newId)
    {
        if (itemDic.ContainsKey(item.itemId))
            itemDic.Remove(item.itemId);

        item.itemId = newId;

        if (!itemDic.ContainsKey(newId))
            itemDic.Add(newId, item);
        else
            itemDic[newId] = item;

        Debug.Log($"✓ OverrideItemId: {item.name} → id={newId}");
    }

    /// <summary>
    /// 네트워크 패킷으로 아이템 처리 (호스트/피어 공용)
    /// - 씬 배치 아이템: 이미 존재하는 오브젝트를 찾아 ID만 교체
    /// - 런타임 스폰 아이템: 새로 Instantiate 후 ID 교체
    /// </summary>
    public void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot)
    {
        // 씬에 이미 배치된 아이템인지 확인 (위치 기반 근접 탐색)
        Items existingItem = FindScenePlacedItem(itemStringKey, pos);
        if (existingItem != null)
        {
            if (itemId > 0)
                OverrideItemId(existingItem, itemId);
            Debug.Log($"[ItemManager] 씬 배치 아이템 ID 동기화: key={itemStringKey}, id={itemId}");
            return;
        }

        // 런타임 스폰 아이템: 새로 생성
        GameObject prefab = GetPrefabByKey(itemStringKey);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager] itemStringKey={itemStringKey}에 해당하는 프리팹이 없습니다. ItemPrefabList를 확인하세요.");
            return;
        }

        GameObject spawnedObj = Instantiate(prefab, pos, rot);

        Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
            Vector3 up = planet != null
                ? planet.GetGravityUp(spawnedObj.transform)
                : Vector3.up;
            rb.AddForce((up + spawnedObj.transform.forward) * -150f);
        }

        Items itemComp = spawnedObj.GetComponent<Items>();
        if (itemComp != null && itemId > 0)
            StartCoroutine(OverrideIdNextFrame(itemComp, itemId));

        Debug.Log($"[ItemManager] SpawnItemFromNetwork: key={itemStringKey}, id={itemId}, pos={pos}");
    }

    // 씬에 이미 배치된 아이템을 stringKey + 위치 근접도로 탐색
    private Items FindScenePlacedItem(string key, Vector3 pos)
    {
        foreach (var item in itemDic.Values)
        {
            if (item.itemStringKey == key && item.IsScenePlacedItem &&
                Vector3.Distance(item.transform.position, pos) < 1f)
                return item;
        }
        return null;
    }

    private IEnumerator OverrideIdNextFrame(Items itemComp, int newId)
    {
        yield return null; // Items.Start()의 RegisterItem() 완료 대기
        OverrideItemId(itemComp, newId);
    }

    public GameObject GetPrefabByKey(string key)
    {
        ItemPrefabData data = itemPrefabList.Find(x => x.itemStringKey == key);
        return data?.prefab;
    }

    /// <summary>
    /// 피어 요청으로 호스트가 아이템을 스폰하고 실제 ID로 전체 브로드캐스트
    /// </summary>
    public void SpawnItemAndBroadcast(string itemStringKey, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = GetPrefabByKey(itemStringKey);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager] SpawnItemAndBroadcast: 프리팹 없음 key={itemStringKey}");
            return;
        }

        GameObject spawnedObj = Instantiate(prefab, pos, rot);

        Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
            Vector3 up = planet != null ? planet.GetGravityUp(spawnedObj.transform) : Vector3.up;
            rb.AddForce((up + spawnedObj.transform.forward) * -150f);
        }

        Items itemComp = spawnedObj.GetComponent<Items>();
        if (itemComp != null)
            StartCoroutine(BroadcastAfterRegistration(itemComp, pos, spawnedObj.transform.rotation));
    }

    // Items.Start() → RegisterItem() 완료 후 실제 ID로 전체 브로드캐스트
    private IEnumerator BroadcastAfterRegistration(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null;
        PacketSender.Instance.SendObjectSpawn(itemComp, pos, rot);
        Debug.Log($"[ItemManager] SpawnItemAndBroadcast 완료: id={itemComp.itemId}, key={itemComp.itemStringKey}");
    }
}
