using Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    // itemStringKey → 프리팹 매핑 테이블 (인스펙터에서 등록)
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
    /// 네트워크 패킷으로 아이템 생성 (호스트/피어 공용)
    /// Items.Start()에서 RegisterItem이 호출되므로 한 프레임 뒤에 OverrideItemId 처리
    /// </summary>
    public void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = GetPrefabByKey(itemStringKey);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager] itemStringKey={itemStringKey}에 해당하는 프리팹이 없습니다. ItemPrefabList를 확인하세요.");
            return;
        }

        GameObject spawnedObj = Instantiate(prefab, pos, rot);

        // Ore.DropItem()과 동일하게 행성 위 방향 + forward로 힘을 줌
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
}
