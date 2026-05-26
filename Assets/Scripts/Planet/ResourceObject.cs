using UnityEngine;

/// <summary>
/// 씬에 배치된 채집 가능한 자원(광석/나무 등)의 공통 베이스.
/// </summary>
public abstract class ResourceObject : MonoBehaviour
{
    [HideInInspector] public int resourceId;

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

    /// <summary>
    /// 행성 중심→바깥(up) 기준 드롭 위치·임펄스 방향.
    /// 표면 레이에 맞으면 그 지점 위에 스폰, 아니면 origin + up * throwHeight.
    /// </summary>
    protected void GetPlanetDropSpawn(float throwHeight, float surfaceOffset, out Vector3 spawnPos, out Vector3 impulseDir)
    {
        PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
        Vector3 up = planet != null ? planet.GetGravityUp(transform) : transform.up;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 1e-4f)
            forward = Vector3.ProjectOnPlane(transform.right, up);
        forward = forward.sqrMagnitude >= 1e-4f ? forward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;

        LayerMask groundMask = LayerMask.GetMask(
            Define.Layer.GROUND, Define.Layer.WALKABLE_COLLIDER, Define.Layer.HILL);

        if (Physics.Raycast(transform.position + up * 0.5f, -up, out RaycastHit hit, 20f, groundMask))
            spawnPos = hit.point + hit.normal * surfaceOffset;
        else
            spawnPos = transform.position + up * throwHeight;

        impulseDir = up + forward;
    }

    protected static System.Collections.IEnumerator BroadcastDropSpawnNextFrame(Items itemComp, string logTag)
    {
        yield return null;
        if (itemComp == null) yield break;

        PacketSender.Instance.SendObjectSpawn(itemComp, itemComp.transform.position, itemComp.transform.rotation);
        Debug.Log($"[{logTag}] SendObjectSpawn: itemId={itemComp.itemId}, key={itemComp.itemStringKey}, pos={itemComp.transform.position}");
    }
}
