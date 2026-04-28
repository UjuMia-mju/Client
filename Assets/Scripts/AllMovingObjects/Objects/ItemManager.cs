using Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    [Header("아이템 카탈로그 (키·표시명·아이콘·프리팹 단일 관리)")]
    [SerializeField] private ItemCatalog itemCatalog;

    private static int _nextItemId = 1;

    private const float PEER_THROW_FORCE = 200f;
    private const float PEER_THROW_DURATION = 0.4f;

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
    /// 네트워크 패킷으로 아이템 처리.
    /// pos = FurnaceClientManager.BroadcastSpawnNextFrame에서 보내는 용광로 스폰 원점
    /// </summary>
    public void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot)
    {
        Debug.Log($"[ItemManager] SpawnItemFromNetwork: key={itemStringKey}, id={itemId}, pos={pos}");

        Items existingItem = FindScenePlacedItem(itemStringKey, pos);
        if (existingItem != null)
        {
            if (itemId > 0)
                OverrideItemId(existingItem, itemId);
            Debug.Log($"[ItemManager] 씬 배치 아이템 ID 동기화: key={itemStringKey}, id={itemId}");
            return;
        }

        GameObject prefab = GetPrefabByKey(itemStringKey);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager] 프리팹 없음: key={itemStringKey}");
            return;
        }

        GameObject spawnedObj = Instantiate(prefab, pos, rot);

        Items itemComp = spawnedObj.GetComponent<Items>();
        if (itemComp != null)
            StartCoroutine(PostSpawnSetup(itemComp, itemId, pos, rot));
    }

    /// <summary>
    /// Items.Start()에서 RegisterItem() 및 isKinematic 설정 완료 후 실행.
    /// 1) ID 교체  2) 용광로 UI 초기화  3) 피어 배출 연출
    /// </summary>
    private IEnumerator PostSpawnSetup(Items itemComp, int newId, Vector3 spawnOrigin, Quaternion spawnRot)
    {
        yield return null; // Items.Start() 완료 대기

        if (itemComp == null) yield break;

        // 1. 아이템 ID를 호스트 기준으로 교체
        if (newId > 0)
            OverrideItemId(itemComp, newId);

        // 2. 스폰 원점 기반 근처 용광로 UI 초기화 (S_FURNACE_RETRIEVE 누락/지연 안전장치)
        FurnaceClientManager.Instance?.TryResetNearestFurnaceBySpawnPosition(spawnOrigin);

        // 3. 피어에서만 배출 연출 적용
        //    Items.Start()에서 isKinematic=true가 된 뒤이므로 여기서 켜야 함
        bool isPeer = ConnectManager.Instance != null && !ConnectManager.Instance.isHost;
        if (isPeer)
            StartCoroutine(ApplyPeerThrowImpulse(itemComp.gameObject, spawnRot));

        Debug.Log($"[ItemManager] PostSpawnSetup 완료: id={newId}, isPeer={isPeer}");
    }

    /// <summary>
    /// 피어 전용 배출 연출.
    /// isKinematic을 잠시 끄고 힘을 가한 뒤 다시 켜서 S_OBJECT_MOVE 동기화 모드로 복귀.
    /// </summary>
    private IEnumerator ApplyPeerThrowImpulse(GameObject obj, Quaternion spawnRot)
    {
        if (obj == null) yield break;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[ItemManager] ApplyPeerThrowImpulse: Rigidbody 없음");
            yield break;
        }

        PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
        Vector3 up = planet != null ? planet.GetGravityUp(obj.transform) : Vector3.up;

        // 용광로에서 뱉어나오는 방향: up + forward (FurnaceObject.ThrowSmeltedItem과 동일)
        Vector3 throwDir = (up + (spawnRot * Vector3.forward)).normalized;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(throwDir * PEER_THROW_FORCE);

        Debug.Log($"[ItemManager] 피어 배출 impulse: dir={throwDir}, force={PEER_THROW_FORCE}");

        yield return new WaitForSeconds(PEER_THROW_DURATION);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

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

    public GameObject GetPrefabByKey(string key)
    {
        if (itemCatalog == null)
            return null;
        return itemCatalog.GetPrefab(key);
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

    private IEnumerator BroadcastAfterRegistration(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null;
        PacketSender.Instance.SendObjectSpawn(itemComp, pos, rot);
        Debug.Log($"[ItemManager] SpawnItemAndBroadcast 완료: id={itemComp.itemId}, key={itemComp.itemStringKey}");
    }
}
