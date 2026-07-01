using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

[DefaultExecutionOrder(-100)]
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    [Header("아이템 카탈로그 (키·표시명·아이콘·프리팹 단일 관리)")]
    [SerializeField] private ItemCatalog itemCatalog;

    public ItemCatalog Catalog => itemCatalog;

    // ── ID 대역 분리 ────────────────────────────────────────────────
    // 씬에 미리 배치된 아이템:   1 ~ (1000 미만)  → 리스트 순서대로 고정 부여.
    // 동적으로 스폰되는 아이템:  4000 이상        → _nextItemId 로 부여.
    // 두 대역이 절대 겹치지 않으므로 네트워크 스폰이 씬 아이템 ID를 침범할 수 없다.
    private const int DynamicIdBase = 4000;
    private int _nextItemId = DynamicIdBase;

    // 피어가 매칭 큐 구성 전에 도착한 S_OBJECT_SPAWN 보류 버퍼.
    [System.Serializable]
    public struct PendingSpawn
    {
        public int itemId;
        public string key;
        public Vector3 pos;
        public Quaternion rot;
        public int attempts;
    }

    private readonly List<PendingSpawn> _pendingNetworkSpawns = new List<PendingSpawn>();
    private bool _scenePlacedRegistered;

    [Header("인스펙터 수동 등록")]
    [Tooltip("씬에 배치된 Items 컴포넌트를 넣으면 리스트 순서대로 1,2,3... ID가 부여됩니다.")]
    public List<Items> inspectorScenePlacedItems = new List<Items>();

    [Tooltip("인스펙터의 ScenePlaced 항목들을 Awake 시 등록합니다.")]
    private bool populateItemDicFromInspector = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 동적 스폰 대역은 항상 4000부터 시작.
        _nextItemId = DynamicIdBase;

        // 인스펙터에 넣은 씬 배치 아이템을 리스트 순서대로 1,2,3... 등록.
        // 씬 아이템은 호스트/피어 양쪽에 이미 똑같이 존재하므로, 각자 로컬에서
        // 같은 순서로 같은 ID를 받는다. 네트워크로 주고받지 않는다.
        if (populateItemDicFromInspector && inspectorScenePlacedItems != null)
        {
            int scenePlacedNextId = 1;
            foreach (Items it in inspectorScenePlacedItems)
            {
                if (it == null) continue;
                if (it.GetComponentInParent<Player>() != null) continue;
                if (it.GetComponentInParent<OtherPlayers>() != null) continue;

                RegisterScenePlacedItem(it, ref scenePlacedNextId);
            }
        }
    }

    private void Update()
    {
        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
            return;
        if (!_scenePlacedRegistered || _pendingNetworkSpawns.Count == 0)
            return;

        RetryPendingNetworkSpawns();
    }

    private IEnumerator Start()
    {
        // ConnectManager.SetHostRole / PacketSender.Init 완료 보장.
        yield return null;

        _scenePlacedRegistered = true;

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;

        // 피어: 등록 전에 먼저 도착한 동적 스폰 패킷 일괄 처리.
        if (!isHost && _pendingNetworkSpawns.Count > 0)
        {
            _pendingNetworkSpawns.Sort(ComparePendingSpawn);
            RetryPendingNetworkSpawns();
        }
    }

    static int ComparePendingSpawn(PendingSpawn a, PendingSpawn b)
    {
        int keyCmp = string.Compare(a.key, b.key, System.StringComparison.Ordinal);
        if (keyCmp != 0) return keyCmp;

        int x = a.pos.x.CompareTo(b.pos.x);
        if (x != 0) return x;
        int y = a.pos.y.CompareTo(b.pos.y);
        if (y != 0) return y;
        return a.pos.z.CompareTo(b.pos.z);
    }

    void RetryPendingNetworkSpawns()
    {
        if (_pendingNetworkSpawns.Count == 0)
            return;

        PendingSpawn[] buffered = _pendingNetworkSpawns.ToArray();
        _pendingNetworkSpawns.Clear();
        foreach (PendingSpawn s in buffered)
            SpawnItemFromNetwork(s.itemId, s.key, s.pos, s.rot, s.attempts);
    }

    // ── 씬 배치 아이템 전용 등록 ──────────────────────────────────────
    // 리스트 순서대로 1,2,3... 을 부여한다. 동적 대역(4000+)과 겹치지 않는다.
    private void RegisterScenePlacedItem(Items item, ref int scenePlacedNextId)
    {
        if (item == null) return;

        if (itemCatalog != null && !string.IsNullOrEmpty(item.itemStringKey))
        {
            if (!itemCatalog.TryGet(item.itemStringKey, out _))
            {
                Debug.LogError($"[ItemManager] '{item.name}' 의 키 '{item.itemStringKey}' 가 ItemCatalog에 없습니다.", item);
                return;
            }
        }

        item.itemId = scenePlacedNextId++;

        if (!itemDic.ContainsKey(item.itemId))
        {
            itemDic.Add(item.itemId, item);
            Debug.Log($"✓ Registered scene item: {item.name} (id={item.itemId}, key={item.itemStringKey})");
        }
        else
        {
            Debug.LogWarning($"[ItemManager] 씬 아이템 id={item.itemId} 중복. 대상: {item.name}");
        }
    }

    // ── 동적 스폰 아이템 등록 (기존 RegisterItem 호환) ──────────────────
    // 동적 대역(4000+)에서 ID를 부여한다.
    public void RegisterItem(Items item)
    {
        if (item == null) return;

        if (itemCatalog != null && !string.IsNullOrEmpty(item.itemStringKey))
        {
            if (!itemCatalog.TryGet(item.itemStringKey, out _))
            {
                Debug.LogError($"[ItemManager] '{item.name}' 의 키 '{item.itemStringKey}' 가 ItemCatalog에 없습니다.", item);
                return;
            }
        }

        item.itemId = _nextItemId++;
        if (!itemDic.ContainsKey(item.itemId))
        {
            itemDic.Add(item.itemId, item);
            Debug.Log($"✓ Registered item: {item.name} (id={item.itemId}, key={item.itemStringKey})");
        }
    }

    public void UnregisterItem(Items item)
    {
        if (item == null) return;
        if (itemDic.TryGetValue(item.itemId, out Items mapped) && ReferenceEquals(mapped, item))
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

    // ── 안전 버전 ID 교체 ─────────────────────────────────────────────
    // 씬 아이템은 이제 네트워크 경로에 들어오지 않으므로, 이 메서드는
    // 동적 아이템(4000+)끼리만 다룬다. 씬 슬롯(1~)을 침범할 일이 없다.
    public void OverrideItemId(Items item, int newId)
    {
        if (item == null) return;

        if (itemDic.TryGetValue(item.itemId, out Items mappedOld) && ReferenceEquals(mappedOld, item))
            itemDic.Remove(item.itemId);

        if (itemDic.TryGetValue(newId, out Items mappedNew) && !ReferenceEquals(mappedNew, item))
        {
            Debug.LogWarning($"[ItemManager] OverrideItemId 충돌: id={newId} 슬롯이 '{mappedNew.name}' 에 잡혀 있어 해제됩니다.");
            mappedNew.itemId = 0;
            itemDic.Remove(newId);
        }

        item.itemId = newId;
        itemDic[newId] = item;
        Debug.Log($"✓ OverrideItemId: {item.name} → id={newId}");
    }

    public void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot)
    {
        SpawnItemFromNetwork(itemId, itemStringKey, pos, rot, 0);
    }

    // ── 네트워크 동적 스폰 ────────────────────────────────────────────
    // ★ 씬 배치 아이템 매칭 로직을 전부 제거했다.
    //   네트워크로 온 스폰은 언제나 새 프리팹을 Instantiate 하는 동적 아이템으로만 취급한다.
    //   → 씬 아이템은 네트워크가 절대 건드리지 않는다. (ID 어긋남 원천 차단)
    void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot, int attempts)
    {
        Debug.Log($"[ItemManager] SpawnItemFromNetwork: key={itemStringKey}, id={itemId}, pos={pos}");

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;

        // 피어: 등록 완료 전에 도착한 스폰은 버퍼링.
        if (!isHost && !_scenePlacedRegistered)
        {
            _pendingNetworkSpawns.Add(new PendingSpawn
            {
                itemId = itemId,
                key = itemStringKey,
                pos = pos,
                rot = rot,
                attempts = attempts
            });
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

    private IEnumerator PostSpawnSetup(Items itemComp, int newId, Vector3 spawnOrigin, Quaternion spawnRot)
    {
        yield return null;

        if (itemComp == null) yield break;

        if (newId > 0)
            OverrideItemId(itemComp, newId);

        FurnaceClientManager.Instance?.TryResetNearestFurnaceBySpawnPosition(spawnOrigin);

        Debug.Log($"[ItemManager] PostSpawnSetup 완료: id={newId}");
    }

    public GameObject GetPrefabByKey(string key)
    {
        if (itemCatalog == null)
            return null;
        return itemCatalog.GetPrefab(key);
    }

    // ── 호스트가 동적 아이템을 스폰하고 브로드캐스트 ──────────────────────
    // 호스트가 4000+ ID를 확정한 뒤 브로드캐스트하므로, 피어들은 같은 ID를 받는다.
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
        {
            // 호스트가 4000+ 대역에서 ID를 확정하고 등록.
            itemComp.itemId = _nextItemId++;
            itemDic[itemComp.itemId] = itemComp;
            StartCoroutine(BroadcastAfterRegistration(itemComp, pos, spawnedObj.transform.rotation));
        }
    }

    private IEnumerator BroadcastAfterRegistration(Items itemComp, Vector3 pos, Quaternion rot)
    {
        yield return null;
        PacketSender.Instance.SendObjectSpawn(itemComp, pos, rot);
        Debug.Log($"[ItemManager] SpawnItemAndBroadcast 완료: id={itemComp.itemId}, key={itemComp.itemStringKey}");
    }
}

// ====================================================================
// [에디터 통합] string 필드를 ItemCatalog 기반 드롭다운으로 그리는 어트리뷰트
// ====================================================================

public class ItemKeyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ItemKeyAttribute))]
internal class ItemKeyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "[ItemKey]는 string 필드에만 사용 가능");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemCatalog");
        if (guids.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.HelpBox(position, "ItemCatalog 에셋을 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
            AssetDatabase.GUIDToAssetPath(guids[0]));

        if (catalog == null || catalog.Entries == null || catalog.Entries.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string[] keys = catalog.Entries
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.key))
            .Select(e => e.key)
            .ToArray();

        int currentIndex = System.Array.IndexOf(keys, property.stringValue);

        string[] displayOptions = keys;
        if (currentIndex < 0 && !string.IsNullOrEmpty(property.stringValue))
        {
            displayOptions = keys.Concat(new[] { $"(Missing) {property.stringValue}" }).ToArray();
            currentIndex = displayOptions.Length - 1;
        }

        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
        if (EditorGUI.EndChangeCheck() && selected >= 0 && selected < keys.Length)
        {
            property.stringValue = keys[selected];
        }
    }
}
#endif