using UnityEngine;

/// <summary>
/// 씬에 배치된 채집 가능한 자원(광석/나무 등)의 공통 베이스.
/// </summary>
public abstract class ResourceObject : MonoBehaviour
{
    [HideInInspector] public int resourceId;

    [Tooltip("프리팹/타입 식별 키. (예: \"ore_iron\", \"tree_oak\")")]
    public string resourceStringKey;

    /// <summary>이 자원에서 총 몇 번 아이템이 떨어진 뒤 사라질지. 서브클래스가 오버라이드하여 인스펙터로 조정.</summary>
    public virtual int MaxDrops => 1;

    protected virtual void Start()
    {
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
}
