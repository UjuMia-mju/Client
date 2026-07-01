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

    private static int _nextItemId = 1;

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

    /// <summary>씬 배치 아이템을 패킷 위치와 매칭할 때 허용 거리(자원 매니저와 동일).</summary>
    const float ScenePlacedMatchMaxDist = 1.0f;
    const int MaxSceneSpawnRetryAttempts = 60;

    [Header("인스펙터 수동 등록 (디버그/테스트용)")]
    [Tooltip("씬에 배치된 Items 컴포넌트를 수동으로 넣어 ItemDic에 등록합니다.")]
    public List<Items> inspectorScenePlacedItems = new List<Items>();

    [Tooltip("네트워크에서 도착한 스폰을 수동으로 버퍼에 넣을 때 사용합니다.")]
    private List<PendingSpawn> inspectorPendingSpawns = new List<PendingSpawn>();

    [Tooltip("인스펙터의 ScenePlaced 항목들을 Awake 시 RegisterItem 으로 자동 등록합니다.")]
    private bool populateItemDicFromInspector = true;

    [Tooltip("인스펙터에 입력한 PendingSpawn 항목들을 Start 시 네트워크 대기 큐로 복사합니다.")]
    private bool enqueuePendingSpawnsFromInspector = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextItemId = 1;

        // 기존 씬에 배치된 Items 수집 (원래 동작 유지).
        Items[] placed = FindObjectsByType<Items>(FindObjectsSortMode.None);
        foreach (Items it in placed)
        {
            if (it == null) continue;
            //if (!it.IsScenePlacedItem) continue;

            // 액터에 붙은 도구(Player/OtherPlayers 자식)는 액터와 함께 모든 머신에 존재.
            // 네트워크 동기화 대상에서 제외 (DesertWorm/플레이어 도구 등).
            if (it.GetComponentInParent<Player>() != null) continue;
            if (it.GetComponentInParent<OtherPlayers>() != null) continue;

            // 기본 구현: 씬 배치 아이템은 Inspector 또는 런타임 RegisterItem 으로 관리.
            // (기존 주석 처리된 _pendingScenePlacedItems 로직 대신 인스펙터 항목을 사용 가능)
        }

        // 인스펙터에 넣은 씬 배치 아이템을 런타임에 등록(옵션)
        if (populateItemDicFromInspector && inspectorScenePlacedItems != null)
        {
            foreach (Items it in inspectorScenePlacedItems)
            {
                if (it == null) continue;
                //if (!it.IsScenePlacedItem) continue;
                if (it.GetComponentInParent<Player>() != null) continue;
                if (it.GetComponentInParent<OtherPlayers>() != null) continue;

                // RegisterItem은 ID를 자동 부여하고 itemDic에 넣습니다.
                RegisterItem(it);
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

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;

        if (isHost)
        {
            // 호스트는 씬 배치 아이템에게 ID를 부여하고 브로드캐스트하는 기존 로직이 여기에 남아있습니다.
        }
        // 피어는 itemId 를 부여하지 않고 매칭 큐로만 둔다.

        _scenePlacedRegistered = true;

        // 인스펙터에 미리 채운 PendingSpawn 을 런타임 버퍼로 복사 (피어에서 주로 사용)
        if (!isHost && enqueuePendingSpawnsFromInspector && inspectorPendingSpawns != null && inspectorPendingSpawns.Count > 0)
        {
            foreach (var ps in inspectorPendingSpawns)
            {
                _pendingNetworkSpawns.Add(ps);
            }
        }

        // 피어: 등록 전에 먼저 도착한 스폰 패킷 일괄 처리
        if (!isHost && _pendingNetworkSpawns.Count > 0)
        {
            _pendingNetworkSpawns.Sort(ComparePendingSpawn);
            RetryPendingNetworkSpawns();
        }
    }

    //static int CompareScenePlacedForSync(Items a, Items b)
    //{
    //    if (a == null && b == null) return 0;
    //    if (a == null) return 1;
    //    if (b == null) return -1;

    //    int keyCmp = string.Compare(a.itemStringKey, b.itemStringKey, System.StringComparison.Ordinal);
    //    if (keyCmp != 0) return keyCmp;

    //    Vector3 pa = a.transform.position;
    //    Vector3 pb = b.transform.position;
    //    int x = pa.x.CompareTo(pb.x);
    //    if (x != 0) return x;
    //    int y = pa.y.CompareTo(pb.y);
    //    if (y != 0) return y;
    //    return pa.z.CompareTo(pb.z);
    //}

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

    public void RegisterItem(Items item)
    {
        if (item == null) return;

        // 카탈로그에 등록된 키인지 검증만 수행
        if (itemCatalog != null && !string.IsNullOrEmpty(item.itemStringKey))
        {
            if (!itemCatalog.TryGet(item.itemStringKey, out _))
            {
                Debug.LogError($"[ItemManager] '{item.name}' 의 키 '{item.itemStringKey}' 가 ItemCatalog에 등록되지 않았습니다.", item);
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
        // ★ dic[item.itemId] 가 다른 아이템일 수 있으므로 값 체크 후 제거.
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

    /// <summary>
    /// ★ 안전 버전: dic 값을 확인하여 다른 아이템의 매핑을 실수로 삭제하지 않는다.
    /// </summary>
    public void OverrideItemId(Items item, int newId)
    {
        if (item == null) return;

        // 1) item 의 기존 dic 매핑이 본인을 가리킬 때에만 제거
        if (itemDic.TryGetValue(item.itemId, out Items mappedOld) && ReferenceEquals(mappedOld, item))
            itemDic.Remove(item.itemId);

        // 2) newId 슬롯에 이미 다른 아이템이 있으면 그 아이템의 itemId 를 0(미할당)으로 되돌리고 로그
        if (itemDic.TryGetValue(newId, out Items mappedNew) && !ReferenceEquals(mappedNew, item))
        {
            Debug.LogWarning(
                $"[ItemManager] OverrideItemId 충돌: id={newId} 슬롯이 '{mappedNew.name}' 에 잡혀 있어 해제됩니다.");
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

    void SpawnItemFromNetwork(int itemId, string itemStringKey, Vector3 pos, Quaternion rot, int attempts)
    {
        Debug.Log($"[ItemManager] SpawnItemFromNetwork: key={itemStringKey}, id={itemId}, pos={pos}");

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;

        // 피어 측: 사전 등록이 끝나기 전에 도착한 스폰은 버퍼링.
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

        // 1) 씬 배치 매칭 큐 우선 검색 (피어 전용)
        Items existingFromPending = FindPendingScenePlacedItem(itemStringKey, pos);
        if (existingFromPending != null)
        {
            existingFromPending.itemId = itemId;
            itemDic[itemId] = existingFromPending;
            existingFromPending.transform.SetPositionAndRotation(pos, rot);
            existingFromPending.HasBeenSyncedFromNetwork = true; // [추가] 재매칭 방지
            Debug.Log($"[ItemManager] 씬 배치 아이템 ID 동기화 (피어): key={itemStringKey}, id={itemId}");
            return;
        }

        // 2) 기존 등록 아이템 중 동일 키 매칭 (이전 동작 호환)
        Items existingItem = FindScenePlacedItem(itemStringKey, pos);
        if (existingItem != null)
        {
            if (itemId > 0)
                OverrideItemId(existingItem, itemId);
            existingItem.HasBeenSyncedFromNetwork = true; // [추가] 재매칭 방지
            Debug.Log($"[ItemManager] 씬 배치 아이템 ID 동기화: key={itemStringKey}, id={itemId}");
            return;
        }

        // 3) 일반 네트워크 스폰 — 씬 배치 후보가 남아 있으면 중복 Instantiate 대신 재시도
        if (!isHost && HasPendingScenePlacedWithKey(itemStringKey))
        {
            if (attempts < MaxSceneSpawnRetryAttempts)
            {
                _pendingNetworkSpawns.Add(new PendingSpawn
                {
                    itemId = itemId,
                    key = itemStringKey,
                    pos = pos,
                    rot = rot,
                    attempts = attempts + 1
                });
                return;
            }

            Debug.LogWarning(
                $"[ItemManager] 씬 배치 매칭 재시도 초과. key={itemStringKey}, id={itemId}, pos={pos}");
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

    private Items FindPendingScenePlacedItem(string key, Vector3 pos)
    {
        // 이전에 별도 큐로 관리하던 pendingScenePlacedItems 는 제거되었습니다.
        // 대신 inspector 또는 런타임에 RegisterItem 된 itemDic 을 우선 검사합니다.
        Items best = null;
        float bestDist = float.MaxValue;

        foreach (var item in itemDic.Values)
        {
            if (item == null) continue;
            //if (!item.IsScenePlacedItem) continue;
            if (item.HasBeenSyncedFromNetwork) continue;
            if (item.itemStringKey != key) continue;

            float d = Vector3.Distance(item.transform.position, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = item;
            }
        }

        if (best != null && bestDist <= ScenePlacedMatchMaxDist)
            return best;

        if (best != null)
        {
            Debug.LogWarning(
                $"[ItemManager] 씬 배치 매칭 거리 초과: key={key}, dist={bestDist:F2}m (max={ScenePlacedMatchMaxDist}m)");
        }

        return null;
    }

    bool HasPendingScenePlacedWithKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        // inspectorScenePlacedItems가 itemDic으로 등록되었다면 FindScenePlacedItem에서 감지됩니다.
        // 추가로 inspector에만 남아있는 항목을 확인하려면 아래 검사(옵션)를 사용할 수 있습니다.
        if (inspectorScenePlacedItems != null)
        {
            foreach (Items it in inspectorScenePlacedItems)
            {
                if (it != null && it.itemStringKey == key)
                    return true;
            }
        }

        return false;
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

    private Items FindScenePlacedItem(string key, Vector3 pos)
    {
        Items best = null;
        float bestDist = float.MaxValue;
        foreach (var item in itemDic.Values)
        {
            if (item == null) continue;
            //if (!item.IsScenePlacedItem) continue;
            if (item.HasBeenSyncedFromNetwork) continue;
            if (item.itemStringKey != key) continue;

            float d = Vector3.Distance(item.transform.position, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = item;
            }
        }
        return best != null && bestDist <= ScenePlacedMatchMaxDist ? best : null;
    }

    public GameObject GetPrefabByKey(string key)
    {
        if (itemCatalog == null)
            return null;
        return itemCatalog.GetPrefab(key);
    }

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
