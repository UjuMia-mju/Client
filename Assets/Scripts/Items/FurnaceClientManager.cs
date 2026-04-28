using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocol;


[System.Serializable]
public class ResultPrefabData
{
    public int resultItemType;
    public GameObject resultPrefab;
}

public class FurnaceClientManager : MonoBehaviorSingleton<FurnaceClientManager>
{
    private Dictionary<int, FurnaceObject> furnaceControllers = new();
    private HashSet<int> retrievedFurnaces = new();

    private const float ITEM_THROW_HEIGHT = 3.5f;
    // 스폰 위치 기반 용광로 탐색 거리 (throw_height보다 넉넉하게)
    private const float FURNACE_SPAWN_RESET_DISTANCE = 8f;

    [Header("Result Prefab Settings")]
    [SerializeField] private List<ResultPrefabData> resultPrefabList = new();

    private void Start()
    {
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent += HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent += HandleSmeltCompleted;
            HostPacketHandler.Instance.OnFurnaceRetrieveEvent += HandleFurnaceRetrieve;
        }
    }

    protected override void OnDestroy()
    {
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent -= HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent -= HandleSmeltCompleted;
            HostPacketHandler.Instance.OnFurnaceRetrieveEvent -= HandleFurnaceRetrieve;
        }
        base.OnDestroy();
    }

    public FurnaceObject GetFurnaceObject(int furnaceId)
    {
        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject obj))
            return obj;

        Debug.LogWarning($"[FurnaceClientManager] {furnaceId}번 용광로를 찾을 수 없습니다.");
        return null;
    }

    // ==========================================
    // 용광로 등록/해제 관리
    // ==========================================
    public void RegisterFurnace(int furnaceId, FurnaceObject furnaceObject)
    {
        if (!furnaceControllers.ContainsKey(furnaceId))
            furnaceControllers.Add(furnaceId, furnaceObject);
    }

    public void UnregisterFurnace(int furnaceId)
    {
        furnaceControllers.Remove(furnaceId);
        retrievedFurnaces.Remove(furnaceId);
    }

    // ==========================================
    // 패킷 이벤트 핸들러 (서버 -> 클라이언트)
    // ==========================================
    private void HandleSmeltStarted(S_OBJECT_SMELT packet)
    {
        int furnaceId = packet.FurnaceId;
        int smeltTime = packet.MeltTime;

        retrievedFurnaces.Remove(furnaceId);

        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObject))
        {
            furnaceObject.OnSmeltStarted(smeltTime);
            Debug.Log($"[FurnaceClientManager] {furnaceId}번 용광로 작동 시작 ({smeltTime}초)");
        }
        else
        {
            Debug.LogWarning($"[FurnaceClientManager] {furnaceId}번 용광로를 찾을 수 없습니다.");
        }
    }

    private void HandleSmeltCompleted(S_SMELT_COMPLETE packet)
    {
        int furnaceId = packet.FurnaceId;

        if (!furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObject))
            return;

        if (retrievedFurnaces.Contains(furnaceId))
        {
            Debug.Log($"[FurnaceClientManager] 늦게 도착한 SmeltComplete 무시 (이미 수거됨): furnaceId={furnaceId}");
            furnaceObject.OnItemRetrieved();
            return;
        }

        furnaceObject.OnSmeltCompleted();
        Debug.Log($"[FurnaceClientManager] {furnaceId}번 용광로 완료 이미지 표시");
    }

    private void HandleFurnaceRetrieve(S_FURNACE_RETRIEVE packet)
    {
        Debug.Log($"[FurnaceClientManager] HandleFurnaceRetrieve 수신. isHost={ConnectManager.Instance?.isHost}, furnaceId={packet.FurnaceId}");

        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost) return;

        int furnaceId = packet.FurnaceId;
        retrievedFurnaces.Add(furnaceId);

        FurnaceObject furnaceObj = GetFurnaceObject(furnaceId);
        if (furnaceObj == null)
        {
            Debug.LogWarning($"[FurnaceClientManager] 수거 대상 용광로 없음: furnaceId={furnaceId}");
            return;
        }

        furnaceObj.OnItemRetrieved();
        Debug.Log($"[FurnaceClientManager] 피어 용광로 UI 초기화 완료: furnaceId={furnaceId}");
    }

    /// <summary>
    /// S_OBJECT_SPAWN 도착 시 S_FURNACE_RETRIEVE 누락/지연 대비 안전장치.
    /// spawnPosition은 반드시 용광로의 원점 스폰 위치여야 한다.
    /// </summary>
    public void TryResetNearestFurnaceBySpawnPosition(Vector3 spawnPosition)
    {
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost) return;

        FurnaceObject nearestFurnace = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var pair in furnaceControllers)
        {
            if (pair.Value == null) continue;
            float dist = Vector3.Distance(pair.Value.transform.position, spawnPosition);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestFurnace = pair.Value;
            }
        }

        Debug.Log($"[FurnaceClientManager] TryReset: nearestDist={nearestDistance:F2}, threshold={FURNACE_SPAWN_RESET_DISTANCE}");

        if (nearestFurnace == null || nearestDistance > FURNACE_SPAWN_RESET_DISTANCE)
            return;

        retrievedFurnaces.Add(nearestFurnace.furnaceId);
        nearestFurnace.OnItemRetrieved();
        Debug.Log($"[FurnaceClientManager] 스폰 기반 용광로 UI 초기화: furnaceId={nearestFurnace.furnaceId}");
    }

    // ==========================================
    // 호스트 전용: 로컬 아이템 생성 + 피어 스폰 브로드캐스트
    // ==========================================
    public void SpawnResultItemLocal(int furnaceId, int resultItemType)
    {
        if (!furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObj))
        {
            Debug.LogWarning($"[FurnaceClientManager] SpawnResultItemLocal: furnaceId={furnaceId} 없음");
            return;
        }

        ResultPrefabData foundData = resultPrefabList.Find(x => x.resultItemType == resultItemType);
        if (foundData == null || foundData.resultPrefab == null)
        {
            Debug.LogError($"[FurnaceClientManager] ResultPrefabList에 타입({resultItemType}) 프리팹 누락!");
            return;
        }

        // 피어에게 전달할 원점 스폰 위치/회전 미리 기록
        // (1프레임 뒤 브로드캐스트할 때 아이템이 이미 날아간 위치 대신 스폰 원점을 전송)
        Vector3 spawnOrigin = furnaceObj.transform.position + furnaceObj.transform.up * ITEM_THROW_HEIGHT;
        Quaternion spawnRot = Quaternion.LookRotation(furnaceObj.transform.forward, furnaceObj.transform.up);

        Items spawnedItem = furnaceObj.ThrowSmeltedItem(foundData.resultPrefab);

        if (spawnedItem != null)
            StartCoroutine(BroadcastSpawnNextFrame(spawnedItem, spawnOrigin, spawnRot));
    }

    /// <summary>
    /// Items.Start()에서 itemId 등록이 완료될 때까지 1프레임 대기 후 브로드캐스트.
    /// 피어가 용광로 스폰 원점에서 아이템을 생성할 수 있도록 원점 위치/회전을 전송한다.
    /// </summary>
    private IEnumerator BroadcastSpawnNextFrame(Items item, Vector3 spawnOrigin, Quaternion spawnRot)
    {
        yield return null;
        if (item == null) yield break;

        PacketSender.Instance.BroadcastObjectSpawn(item, spawnOrigin, spawnRot);
        Debug.Log($"[FurnaceClientManager] 스폰 브로드캐스트: itemId={item.itemId}, origin={spawnOrigin}");
    }
}
