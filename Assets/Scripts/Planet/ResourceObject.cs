using UnityEngine;

/// <summary>
/// 씬에 배치된 채집 가능한 자원(광석/나무 등)의 공통 베이스.
/// </summary>
public abstract class ResourceObject : MonoBehaviour
{
    [HideInInspector] public int resourceId;
    /// <summary>
    /// 피어에서 씬 배치 자원 ID가 호스트 값으로 1회 이상 동기화되었는지.
    /// true면 재매칭 후보에서 제외된다.
    /// </summary>
    [System.NonSerialized] public bool HasBeenSyncedFromNetwork = false;

    [Tooltip("프리팹/타입 식별 키. ResourceManager의 Resource Prefab Table 드롭다운에서 선택.")]
    [SerializeField, ResourceKey] private string resourceKey;

    /// <summary>식별 키 (string).</summary>
    public string resourceStringKey => resourceKey;

    /// <summary>이 자원에서 총 몇 번 아이템이 떨어진 뒤 사라질지. 서브클래스가 오버라이드하여 인스펙터로 조정.</summary>
    public virtual int MaxDrops => 1;

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(resourceKey))
        {
            Debug.LogError($"[ResourceObject] '{name}'에 ResourceKey가 지정되지 않았습니다. 프리팹 인스펙터에서 설정하세요.", this);
            return;
        }

        ResourceManager.Instance.RegisterResource(this);

        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            StartCoroutine(ResourceManager.Instance.SyncScenePlacedResourceNextFrame(this));
    }

    protected virtual void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.UnregisterResource(this);
    }

    /// <summary>도구가 자원을 1회 타격했을 때 호출.</summary>
    public abstract void OnHit();

    /// <summary>호스트 권위 측에서 N회 누적 시 아이템을 떨어뜨리는 실제 로직.</summary>
    public abstract void SpawnDropAndBroadcast();

    /// <summary>행성 중심 → 자원 위치 방향(표면 바깥). PlanetGravity가 없으면 transform.up.</summary>
    protected Vector3 GetPlanetOutwardUp()
    {
        PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
        return planet != null ? planet.GetGravityUp(transform) : transform.up;
    }
}
