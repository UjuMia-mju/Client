using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

/// <summary>
/// 씬에 배치된 채집 가능한 자원(광석/나무 등)을 ID로 관리합니다.
/// ItemManager와 동일한 패턴:
///  - 자동 증가 ID 부여
///  - 씬 배치 자원의 ID를 호스트 기준으로 피어와 동기화
///  - 네트워크 파괴 패킷 수신 시 로컬 GameObject 제거
/// 채굴/벌목 진행과 아이템 드롭은 ResourceServerManager(호스트 권위)에서 담당.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private readonly Dictionary<int, ResourceObject> _resourceDic = new Dictionary<int, ResourceObject>();

    [Header("자원 카탈로그 (키·프리팹 단일 관리)")]
    [SerializeField] private ResourceCatalog resourceCatalog;

    public ResourceCatalog Catalog => resourceCatalog;

    private static int _nextResourceId = 1;
    private bool _scenePlacedRegistered;

    // 피어에서 씬 배치 자원과 호스트 자원 ID를 1:1 매칭하기 위한 대기 목록.
    private readonly List<ResourceObject> _pendingScenePlacedResources = new List<ResourceObject>();

    // 피어가 자원 등록을 마치기 전에 도착한 S_RESOURCE_SPAWN 보류 버퍼.
    private struct PendingResourceSpawn
    {
        public int resourceId;
        public string key;
        public Vector3 pos;
        public int attempts;
    }
    private readonly List<PendingResourceSpawn> _pendingNetworkResourceSpawns = new List<PendingResourceSpawn>();
    private readonly HashSet<int> _pendingNetworkResourceDestroys = new HashSet<int>();
    private const int MAX_SYNC_RETRY_ATTEMPTS = 60; // 약 1초(@60fps) 재시도

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextResourceId = 1;

        ResourceObject[] placed = FindObjectsByType<ResourceObject>(FindObjectsSortMode.None);
        foreach (ResourceObject r in placed)
        {
            if (r == null) continue;
            _pendingScenePlacedResources.Add(r);
        }
    }

    private IEnumerator Start()
    {
        // ResourceObject.Start 등록 완료 대기
        yield return null;
        _scenePlacedRegistered = true;

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;
        if (!isHost && _pendingNetworkResourceSpawns.Count > 0)
        {
            PendingResourceSpawn[] buffered = _pendingNetworkResourceSpawns.ToArray();
            _pendingNetworkResourceSpawns.Clear();
            foreach (PendingResourceSpawn s in buffered)
                ApplyResourceIdFromNetwork(s.resourceId, s.key, s.pos);
        }
    }

    private void Update()
    {
        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;
        if (isHost || !_scenePlacedRegistered)
            return;

        if (_pendingNetworkResourceSpawns.Count > 0)
            RetryPendingResourceSpawns();

        if (_pendingNetworkResourceDestroys.Count > 0)
            RetryPendingResourceDestroys();
    }

    // ======================================================================
    // 등록/해제
    // ======================================================================
    public void RegisterResource(ResourceObject resource)
    {
        // 카탈로그에 등록된 키인지 검증만 수행
        if (resourceCatalog != null && !string.IsNullOrEmpty(resource.resourceStringKey))
        {
            if (!resourceCatalog.TryGet(resource.resourceStringKey, out _))
            {
                Debug.LogError($"[ResourceManager] '{resource.name}' 의 키 '{resource.resourceStringKey}' 가 ResourceCatalog에 등록되지 않았습니다.", resource);
                return;
            }
        }

        resource.resourceId = _nextResourceId++;
        if (!_resourceDic.ContainsKey(resource.resourceId))
        {
            _resourceDic.Add(resource.resourceId, resource);
            Debug.Log($"[ResourceManager] ✓ Registered resource: {resource.name} (id={resource.resourceId}, key={resource.resourceStringKey})");
        }
    }

    public void UnregisterResource(ResourceObject resource)
    {
        if (resource == null) return;
        if (_resourceDic.ContainsKey(resource.resourceId))
        {
            _resourceDic.Remove(resource.resourceId);
            Debug.Log($"[ResourceManager] ✓ Unregistered resource: {resource.name} (id={resource.resourceId})");
        }
        _pendingScenePlacedResources.Remove(resource);
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

    /// <summary>호스트가 보낸 ID로 로컬 자원의 ID를 교체. 씬 배치 자원 동기화 용.</summary>
    public void OverrideResourceId(ResourceObject resource, int newId)
    {
        if (resource == null) return;

        if (_resourceDic.ContainsKey(resource.resourceId))
            _resourceDic.Remove(resource.resourceId);

        resource.resourceId = newId;

        if (!_resourceDic.ContainsKey(newId))
            _resourceDic.Add(newId, resource);
        else
            _resourceDic[newId] = resource;

        Debug.Log($"[ResourceManager] ✓ OverrideResourceId: {resource.name} → id={newId}");
    }

    // ======================================================================
    // 씬 배치 자원 동기화
    // ======================================================================
    /// <summary>
    /// 호스트가 자기 씬의 자원 ID를 피어에게 일괄 동기화.
    /// ResourceObject.Start() → 호스트인 경우 이 코루틴이 시작됨.
    /// </summary>
    public IEnumerator SyncScenePlacedResourceNextFrame(ResourceObject resource)
    {
        yield return null; // RegisterResource 완료 보장
        if (resource == null) yield break;

        PacketSender.Instance.BroadcastResourceSpawn(resource);
        Debug.Log($"[ResourceManager] BroadcastResourceSpawn: id={resource.resourceId}, key={resource.resourceStringKey}");
    }

    /// <summary>
    /// 피어 측: 호스트로부터 받은 씬 배치 자원 ID를 로컬에 적용.
    /// pos 기반으로 같은 자원을 찾아 ID를 덮어쓴다.
    /// </summary>
    public void ApplyResourceIdFromNetwork(int resourceId, string resourceStringKey, Vector3 pos)
    {
        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;
        if (!isHost && !_scenePlacedRegistered)
        {
            _pendingNetworkResourceSpawns.Add(new PendingResourceSpawn
            {
                resourceId = resourceId,
                key = resourceStringKey,
                pos = pos,
                attempts = 0
            });
            return;
        }

        ResourceObject existing = FindPendingScenePlacedResource(resourceStringKey, pos);
        if (existing != null)
        {
            OverrideResourceId(existing, resourceId);
            existing.HasBeenSyncedFromNetwork = true;
            _pendingScenePlacedResources.Remove(existing);
            return;
        }

        existing = FindScenePlacedResource(resourceStringKey, pos);
        if (existing == null)
        {
            if (!isHost)
            {
                _pendingNetworkResourceSpawns.Add(new PendingResourceSpawn
                {
                    resourceId = resourceId,
                    key = resourceStringKey,
                    pos = pos,
                    attempts = 1
                });
            }
            Debug.LogWarning($"[ResourceManager] ApplyResourceIdFromNetwork: 매칭 자원 없음(재시도 예정). key={resourceStringKey}, id={resourceId}, pos={pos}");
            return;
        }

        OverrideResourceId(existing, resourceId);
        existing.HasBeenSyncedFromNetwork = true;
        TryApplyPendingDestroy(resourceId);
    }

    private ResourceObject FindPendingScenePlacedResource(string key, Vector3 pos)
    {
        const float SCENE_MATCH_MAX_DIST = 1.0f;

        ResourceObject best = null;
        float bestDist = float.MaxValue;

        foreach (ResourceObject r in _pendingScenePlacedResources)
        {
            if (r == null) continue;
            if (r.HasBeenSyncedFromNetwork) continue;
            if (r.resourceStringKey != key) continue;

            float d = Vector3.Distance(r.transform.position, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = r;
            }
        }

        return best != null && bestDist <= SCENE_MATCH_MAX_DIST ? best : null;
    }

    private ResourceObject FindScenePlacedResource(string key, Vector3 pos)
    {
        const float SCENE_MATCH_MAX_DIST = 1.0f;

        ResourceObject best = null;
        float bestDist = float.MaxValue;
        foreach (var r in _resourceDic.Values)
        {
            if (r == null) continue;
            if (r.HasBeenSyncedFromNetwork) continue;
            if (r.resourceStringKey != key) continue;

            float d = Vector3.Distance(r.transform.position, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = r;
            }
        }
        return best != null && bestDist <= SCENE_MATCH_MAX_DIST ? best : null;
    }

    // ======================================================================
    // 네트워크 파괴 처리
    // ======================================================================
    /// <summary>
    /// 피어 측: 호스트로부터 S_RESOURCE_DESTROY 수신 시 호출.
    /// 호스트 측: ResourceServerManager가 호출하여 로컬도 동일하게 정리.
    /// </summary>
    public void DestroyResourceFromNetwork(int resourceId)
    {
        ResourceObject resource = GetResource(resourceId);
        if (resource == null)
        {
            bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;
            if (!isHost)
            {
                _pendingNetworkResourceDestroys.Add(resourceId);
                Debug.LogWarning($"[ResourceManager] DestroyResourceFromNetwork: id={resourceId} 없음(동기화 대기 보류)");
                return;
            }

            Debug.LogWarning($"[ResourceManager] DestroyResourceFromNetwork: id={resourceId} 없음");
            return;
        }

        UnregisterResource(resource);
        Destroy(resource.gameObject);
        Debug.Log($"[ResourceManager] 자원 파괴: id={resourceId}");
    }

    private void RetryPendingResourceSpawns()
    {
        List<PendingResourceSpawn> survivors = new List<PendingResourceSpawn>();

        for (int i = 0; i < _pendingNetworkResourceSpawns.Count; i++)
        {
            PendingResourceSpawn p = _pendingNetworkResourceSpawns[i];
            ResourceObject matched = FindPendingScenePlacedResource(p.key, p.pos);
            if (matched == null)
                matched = FindScenePlacedResource(p.key, p.pos);

            if (matched != null)
            {
                OverrideResourceId(matched, p.resourceId);
                matched.HasBeenSyncedFromNetwork = true;
                _pendingScenePlacedResources.Remove(matched);
                TryApplyPendingDestroy(p.resourceId);
                continue;
            }

            p.attempts++;
            if (p.attempts < MAX_SYNC_RETRY_ATTEMPTS)
            {
                survivors.Add(p);
            }
            else
            {
                Debug.LogWarning($"[ResourceManager] Resource ID 동기화 최종 실패: key={p.key}, id={p.resourceId}, pos={p.pos}");
            }
        }

        _pendingNetworkResourceSpawns.Clear();
        _pendingNetworkResourceSpawns.AddRange(survivors);
    }

    private void RetryPendingResourceDestroys()
    {
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

    private void TryApplyPendingDestroy(int resourceId)
    {
        if (!_pendingNetworkResourceDestroys.Contains(resourceId))
            return;

        ResourceObject resource = GetResource(resourceId);
        if (resource == null)
            return;

        UnregisterResource(resource);
        Destroy(resource.gameObject);
        _pendingNetworkResourceDestroys.Remove(resourceId);
        Debug.Log($"[ResourceManager] 동기화 직후 보류 파괴 적용: id={resourceId}");
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
