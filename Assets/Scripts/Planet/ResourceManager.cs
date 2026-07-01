using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

/// 씬에 배치된 채집 가능한 자원(광석/나무 등)을 ID로 관리합니다.
///
/// [정리된 방식 — ItemManager와 동일]
///  - 씬 자원은 호스트/피어 양쪽 씬에 이미 똑같이 존재하므로 네트워크로 ID를 주고받지 않는다.
///  - 대신 인스펙터 리스트(inspectorScenePlacedResources) 순서대로 1,2,3... 고정 ID를 부여한다.
///    → 같은 씬을 쓰는 한 호스트/피어가 항상 같은 순서로 같은 ID를 받는다.
///  - 거리(pos) 기반 네트워크 매칭 로직은 전부 제거 (ID 어긋남 원천 차단).
///  - 파괴는 ID로 처리하며, 양쪽이 같은 ID를 가지므로 그대로 동작한다.
///
/// 채굴/벌목 진행과 아이템 드롭은 ResourceServerManager(호스트 권위)에서 담당.

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private readonly Dictionary<int, ResourceObject> _resourceDic = new Dictionary<int, ResourceObject>();

    [Header("자원 카탈로그 (키·프리팹 단일 관리)")]
    [SerializeField] private ResourceCatalog resourceCatalog;

    public ResourceCatalog Catalog => resourceCatalog;

    [Header("인스펙터 수동 등록")]
    [Tooltip("씬에 배치된 ResourceObject를 넣으면 리스트 순서대로 1,2,3... ID가 부여됩니다.\n호스트/피어 양쪽에서 같은 순서 = 같은 ID.")]
    public List<ResourceObject> inspectorScenePlacedResources = new List<ResourceObject>();

    [Tooltip("인스펙터의 자원 항목들을 Awake 시 등록합니다.")]
    private bool populateResourceDicFromInspector = true;

    // 피어가 등록을 마치기 전에 도착한 파괴 패킷 보류 버퍼.
    private readonly HashSet<int> _pendingNetworkResourceDestroys = new HashSet<int>();
    private bool _scenePlacedRegistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 씬 자원을 인스펙터 리스트 순서대로 1,2,3... 등록.
        // 씬 자원은 호스트/피어 양쪽에 이미 존재하므로 네트워크로 주고받지 않는다.
        if (populateResourceDicFromInspector && inspectorScenePlacedResources != null)
        {
            int scenePlacedNextId = 1;
            foreach (ResourceObject r in inspectorScenePlacedResources)
            {
                if (r == null) continue;
                RegisterScenePlacedResource(r, ref scenePlacedNextId);
            }
        }
    }

    private IEnumerator Start()
    {
        // ResourceObject.Start 등록 완료 대기 / ConnectManager 초기화 보장.
        yield return null;
        _scenePlacedRegistered = true;

        // 피어: 등록 전에 먼저 도착한 파괴 패킷 일괄 처리.
        if (_pendingNetworkResourceDestroys.Count > 0)
            RetryPendingResourceDestroys();
    }

    private void Update()
    {
        if (!_scenePlacedRegistered)
            return;

        if (_pendingNetworkResourceDestroys.Count > 0)
            RetryPendingResourceDestroys();
    }

    // ======================================================================
    // 등록/해제
    // ======================================================================

    /// <summary>
    /// 씬 배치 자원 전용 등록. 리스트 순서대로 1,2,3... 부여.
    /// </summary>
    private void RegisterScenePlacedResource(ResourceObject resource, ref int scenePlacedNextId)
    {
        if (resource == null) return;

        if (resourceCatalog != null && !string.IsNullOrEmpty(resource.resourceStringKey))
        {
            if (!resourceCatalog.TryGet(resource.resourceStringKey, out _))
            {
                Debug.LogError($"[ResourceManager] '{resource.name}' 의 키 '{resource.resourceStringKey}' 가 ResourceCatalog에 없습니다.", resource);
                return;
            }
        }

        resource.resourceId = scenePlacedNextId++;

        if (!_resourceDic.ContainsKey(resource.resourceId))
        {
            _resourceDic.Add(resource.resourceId, resource);
            Debug.Log($"[ResourceManager] ✓ Registered scene resource: {resource.name} (id={resource.resourceId}, key={resource.resourceStringKey})");
        }
        else
        {
            Debug.LogWarning($"[ResourceManager] 씬 자원 id={resource.resourceId} 중복. 대상: {resource.name}");
        }
    }

    /// <summary>
    /// 런타임 동적 등록(필요 시). 씬 자원 대역(1~) 위쪽에서 부여.
    /// 현재 구조상 씬 자원만 다루면 거의 쓰이지 않지만 호환용으로 남김.
    /// </summary>
    public void RegisterResource(ResourceObject resource)
    {
        if (resource == null) return;

        if (resourceCatalog != null && !string.IsNullOrEmpty(resource.resourceStringKey))
        {
            if (!resourceCatalog.TryGet(resource.resourceStringKey, out _))
            {
                Debug.LogError($"[ResourceManager] '{resource.name}' 의 키 '{resource.resourceStringKey}' 가 ResourceCatalog에 없습니다.", resource);
                return;
            }
        }

        // 이미 유효한 ID가 있으면 그대로 등록, 없으면 리스트 최대값 뒤에 부여.
        if (resource.resourceId <= 0)
        {
            int maxId = 0;
            foreach (var k in _resourceDic.Keys) if (k > maxId) maxId = k;
            resource.resourceId = maxId + 1;
        }

        if (!_resourceDic.ContainsKey(resource.resourceId))
        {
            _resourceDic.Add(resource.resourceId, resource);
            Debug.Log($"[ResourceManager] ✓ Registered resource: {resource.name} (id={resource.resourceId}, key={resource.resourceStringKey})");
        }
    }

    public void UnregisterResource(ResourceObject resource)
    {
        if (resource == null) return;
        // dic[id] 가 다른 자원일 수 있으므로 값 체크 후 제거.
        if (_resourceDic.TryGetValue(resource.resourceId, out ResourceObject mapped) && ReferenceEquals(mapped, resource))
        {
            _resourceDic.Remove(resource.resourceId);
            Debug.Log($"[ResourceManager] ✓ Unregistered resource: {resource.name} (id={resource.resourceId})");
        }
    }

    public ResourceObject GetResource(int id)
    {
        return _resourceDic.TryGetValue(id, out ResourceObject r) ? r : null;
    }

    public ResourceObject GetResourceByStringKey(string key)
    {
        foreach (var r in _resourceDic.Values)
        {
            if (r.resourceStringKey == key)
                return r;
        }
        return null;
    }

    public IEnumerable<ResourceObject> AllResources => _resourceDic.Values;

    // ======================================================================
    // 네트워크 파괴 처리
    // ======================================================================
    /// <summary>
    /// 피어 측: 호스트로부터 S_RESOURCE_DESTROY 수신 시 호출.
    /// 호스트 측: ResourceServerManager가 호출하여 로컬도 동일하게 정리.
    /// 씬 자원은 양쪽이 같은 ID를 가지므로 ID만으로 안전하게 파괴된다.
    /// </summary>
    public void DestroyResourceFromNetwork(int resourceId)
    {
        ResourceObject resource = GetResource(resourceId);
        if (resource == null)
        {
            // 아직 등록 전이면 보류했다가 등록 후 처리.
            if (!_scenePlacedRegistered)
            {
                _pendingNetworkResourceDestroys.Add(resourceId);
                Debug.LogWarning($"[ResourceManager] DestroyResourceFromNetwork: id={resourceId} 없음(등록 대기 보류)");
                return;
            }

            Debug.LogWarning($"[ResourceManager] DestroyResourceFromNetwork: id={resourceId} 없음");
            return;
        }

        UnregisterResource(resource);
        Destroy(resource.gameObject);
        Debug.Log($"[ResourceManager] 자원 파괴: id={resourceId}");
    }

    private void RetryPendingResourceDestroys()
    {
        if (_pendingNetworkResourceDestroys.Count == 0)
            return;

        List<int> resolved = new List<int>();
        foreach (int resourceId in _pendingNetworkResourceDestroys)
        {
            ResourceObject resource = GetResource(resourceId);
            if (resource == null) continue;

            UnregisterResource(resource);
            Destroy(resource.gameObject);
            Debug.Log($"[ResourceManager] 보류된 자원 파괴 적용: id={resourceId}");
            resolved.Add(resourceId);
        }

        for (int i = 0; i < resolved.Count; i++)
            _pendingNetworkResourceDestroys.Remove(resolved[i]);
    }

    // ======================================================================
    // 프리팹 조회 (필요 시 사용)
    // ======================================================================
    public GameObject GetPrefabByKey(string key)
    {
        if (resourceCatalog == null) return null;
        return resourceCatalog.GetPrefab(key);
    }
}

// ====================================================================
// [에디터 통합] string 필드를 ResourceCatalog 기반 드롭다운으로 그리는 어트리뷰트
// 사용법: [SerializeField, ResourceKey] private string resourceKey;
// ====================================================================

/// <summary>string 필드 위에 붙이면 ResourceCatalog 의 키 드롭다운으로 인스펙터 표시.</summary>
public class ResourceKeyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ResourceKeyAttribute))]
internal class ResourceKeyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "[ResourceKey]는 string 필드에만 사용 가능");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ResourceCatalog");
        if (guids.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.HelpBox(position, "ResourceCatalog 에셋을 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        ResourceCatalog catalog = AssetDatabase.LoadAssetAtPath<ResourceCatalog>(
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