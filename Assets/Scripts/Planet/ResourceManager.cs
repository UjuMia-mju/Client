using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [System.Serializable]
    public class ResourcePrefabData
    {
        public string resourceStringKey;
        public GameObject prefab;
    }

    [Header("Resource Prefab Table")]
    [SerializeField] private List<ResourcePrefabData> resourcePrefabList = new List<ResourcePrefabData>();

    private static int _nextResourceId = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextResourceId = 1;
    }

    // ======================================================================
    // 등록/해제
    // ======================================================================
    public void RegisterResource(ResourceObject resource)
    {
        resource.resourceId = _nextResourceId++;
        if (!_resourceDic.ContainsKey(resource.resourceId))
        {
            _resourceDic.Add(resource.resourceId, resource);
            Debug.Log($"[ResourceManager] ✓ Registered resource: {resource.name} (id={resource.resourceId})");
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
        ResourceObject existing = FindScenePlacedResource(resourceStringKey, pos);
        if (existing == null)
        {
            Debug.LogWarning($"[ResourceManager] ApplyResourceIdFromNetwork: 매칭 자원 없음. key={resourceStringKey}, pos={pos}");
            return;
        }

        OverrideResourceId(existing, resourceId);
    }

    private ResourceObject FindScenePlacedResource(string key, Vector3 pos)
    {
        foreach (var r in _resourceDic.Values)
        {
            if (r == null) continue;
            if (r.resourceStringKey == key && Vector3.Distance(r.transform.position, pos) < 1f)
                return r;
        }
        return null;
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
            Debug.LogWarning($"[ResourceManager] DestroyResourceFromNetwork: id={resourceId} 없음");
            return;
        }

        UnregisterResource(resource);
        Destroy(resource.gameObject);
        Debug.Log($"[ResourceManager] 자원 파괴: id={resourceId}");
    }

    // ======================================================================
    // 프리팹 조회 (필요 시 사용)
    // ======================================================================
    public GameObject GetPrefabByKey(string key)
    {
        ResourcePrefabData data = resourcePrefabList.Find(x => x.resourceStringKey == key);
        return data?.prefab;
    }
}
