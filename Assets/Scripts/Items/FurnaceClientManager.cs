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
    // 씬에 존재하는 용광로 컨트롤러들을 관리하는 딕셔너리
    private Dictionary<int, FurnaceObject> furnaceControllers = new ();
    private const float ITEM_THROW_HEIGHT = 3.5f; 
    private const float ITEM_THROW_FORCE = 200f;

    [Header("Result Prefab Settings")]
    [SerializeField] private List<ResultPrefabData> resultPrefabList = new();

    private void Start()
    {
        // HostPacketHandler의 패킷 수신 이벤트 구독
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent += HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent += HandleSmeltCompleted;
            HostPacketHandler.Instance.OnFurnaceRetrieveEvent += HandleFurnaceRetrieve;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent -= HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent -= HandleSmeltCompleted;
            HostPacketHandler.Instance.OnFurnaceRetrieveEvent -= HandleFurnaceRetrieve;
        }
    }

    public FurnaceObject GetFurnaceObject(int furnaceId)
    {
        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject obj))
        {
            return obj;
        }

        Debug.LogWarning($"[FurnaceClientManager] {furnaceId}번 용광로를 찾을 수 없습니다.");
        return null;
    }

    // ==========================================
    // 용광로 등록/해제 관리
    // ==========================================
    public void RegisterFurnace(int furnaceId, FurnaceObject furnaceObject)
    {
        if (!furnaceControllers.ContainsKey(furnaceId))
        {
            furnaceControllers.Add(furnaceId, furnaceObject);
        }
    }

    public void UnregisterFurnace(int furnaceId)
    {
        if (furnaceControllers.ContainsKey(furnaceId))
        {
            furnaceControllers.Remove(furnaceId);
        }
    }

    // ==========================================
    // 패킷 이벤트 핸들러 (서버 -> 클라이언트)
    // ==========================================
    private void HandleSmeltStarted(S_OBJECT_SMELT packet)
    {
        int furnaceId = packet.FurnaceId;
        int smeltTime = packet.MeltTime;

        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObject))
        {
            // 해당 용광로의 파티클, 사운드 등 작동 시작 연출 (UI적인 처리)
            furnaceObject.OnSmeltStarted(smeltTime);
            Debug.Log($"[Client] {furnaceId}번 용광로 작동 시작 (대기 시간: {smeltTime}초)");
        }
        else
        {
            Debug.LogWarning($"[Client] {furnaceId}번 용광로 컨트롤러를 찾을 수 없습니다.");
        }
    }

    private void HandleSmeltCompleted(S_SMELT_COMPLETE packet)
    {
        int furnaceId = packet.FurnaceId;

        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObject))
        {
            furnaceObject.OnSmeltCompleted();
        }
    }

    // 서버로부터 용광로에서 아이템이 완성되어 수거하라는 패킷을 받았을 때 처리
    private void HandleFurnaceRetrieve(S_FURNACE_RETRIEVE packet)
    {
        int furnaceId = packet.FurnaceId;
        int resultItemType = (int)packet.ItemResult;

        if (ConnectManager.Instance.isHost) return;

        FurnaceObject furnaceObj = GetFurnaceObject(furnaceId);
        if (furnaceObj == null) return;

        furnaceObj.OnSmeltCompleted();

        ResultPrefabData foundData = resultPrefabList.Find(x => x.resultItemType == resultItemType);
        if (foundData == null || foundData.resultPrefab == null)
        {
            Debug.LogError($"[FurnaceClientManager] 타입({resultItemType})의 프리팹이 누락되었습니다!");
            return;
        }

        // ThrowSmeltedItem과 동일한 방식으로 직접 생성 (용광로 기준 방향)
        furnaceObj.ThrowSmeltedItem(foundData.resultPrefab);
    }

    public void SpawnResultItemLocal(int furnaceId, int resultItemType)
    {
        // 1. 해당 ID의 용광로 확인
        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObj))
        {
            // 2. 인스펙터에 등록된 리스트에서 아이템 ID 탐색
            ResultPrefabData foundData = resultPrefabList.Find(x => x.resultItemType == resultItemType);

            if (foundData != null && foundData.resultPrefab != null)
            {
                // 3. 실제 배출 (던지기)
                furnaceObj.ThrowSmeltedItem(foundData.resultPrefab);
            }
            else
            {
                Debug.LogError($"[ClientManager] 인스펙터(ResultPrefabList)에 타입({resultItemType})의 프리팹이 누락되었습니다!");
            }
        }
    }
}
