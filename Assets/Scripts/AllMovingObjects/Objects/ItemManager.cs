using Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    [Header("아이템 카탈로그 (키·표시명·아이콘·프리팹 단일 관리)")]
    [SerializeField] private ItemCatalog itemCatalog;

    public ItemCatalog Catalog => itemCatalog;

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
        // 카탈로그에 등록된 키인지 검증만 수행 (키 자체는 Items가 인스펙터에서 직접 가짐)
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
// 사용법: [SerializeField, ItemKey] private string itemKey;
// ====================================================================

/// <summary>string 필드 위에 붙이면 ItemCatalog 엔트리 드롭다운으로 인스펙터 표시.</summary>
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

        // 프로젝트 내 ItemCatalog 에셋 검색 (첫 번째 사용)
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

        // 카탈로그 키 목록 추출
        string[] keys = catalog.Entries
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.key))
            .Select(e => e.key)
            .ToArray();

        // 현재 값의 인덱스 찾기 (없으면 -1 → 미선택 표시)
        int currentIndex = System.Array.IndexOf(keys, property.stringValue);

        // 카탈로그에서 사라진 값이면 "(Missing)" 항목 임시 추가
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
