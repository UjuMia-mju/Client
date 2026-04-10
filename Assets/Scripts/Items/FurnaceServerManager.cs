using System.Collections;
using System.Collections.Generic;
using Protocol;
using UnityEngine;

// 서버에서 존재하는 모든 용광로의 작업을 총괄하는 매니저
public class FurnaceServerManager : MonoBehaviorSingleton<FurnaceServerManager>
{
    // 용광로 ID(furnaceId)를 키로 하여 현재 진행 중인 제련 코루틴을 추적
    private Dictionary<int, Coroutine> activeFurnaces = new ();
    // 제련이 완료되어 수거를 기다리는 용광로의 결과물(ItemID)을 저장하는 딕셔너리
    private Dictionary<int, int> completedFurnaces = new();
    private void Start()
    {
        PeerPacketHandler.Instance.OnPeerSmeltRequestEvent += OnReceiveSmeltRequest;
        PeerPacketHandler.Instance.OnPeerFurnaceRetrieveEvent += OnReceiveFurnaceRetrieve;
    }

    private void OnDestroy()
    {
        PeerPacketHandler.Instance.OnPeerSmeltRequestEvent -= OnReceiveSmeltRequest;
        PeerPacketHandler.Instance.OnPeerFurnaceRetrieveEvent -= OnReceiveFurnaceRetrieve;
    }

    // 클라이언트로부터 C_OBJECT_SMELT 패킷을 받았을 때 호출 (어떤 용광로인지 정보가 필요함)
    public void OnReceiveSmeltRequest(int furnaceId, ulong objectId)
    {
        // 1. 해당 용광로가 이미 작동 중인지 확인
        if (activeFurnaces.ContainsKey(furnaceId))
        {
            Debug.LogWarning($"[Server] 용광로({furnaceId})는 이미 작동 중입니다.");
            return;
        }

        if (completedFurnaces.ContainsKey(furnaceId))
        {
            Debug.LogWarning($"[Server] 용광로({furnaceId})에는 아직 수거하지 않은 아이템이 있습니다.");
            return;
        }

        Items item = ItemManager.Instance.GetItem((int)objectId);
        if (item == null) return;

        if (SmeltingRecipeManager.Instance.TryGetRecipe(item.itemStringKey, out SmeltingRecipe recipe))
        {
            // 중앙 매니저에서 코루틴 시작 후 딕셔너리에 등록
            Coroutine routine = StartCoroutine(SmeltingRoutine(furnaceId, objectId, recipe));
            activeFurnaces.Add(furnaceId, routine);
            Debug.Log($"[Server] 용광로({furnaceId})에서 제련 시작: 아이템 {item.itemStringKey} -> 결과 {recipe.outputItemID}, 소요 시간 {recipe.smeltingTime}초");
        }
        else
        {
            Debug.LogWarning($"[Server] 아이템({item.itemStringKey})은 녹일 수 없습니다.");
        }
    }

    private IEnumerator SmeltingRoutine(int furnaceId, ulong objectId, SmeltingRecipe recipe)
    {
        // 1. 다른 클라이언트들에게 작업 시작 패킷 브로드캐스트
        PacketSender.Instance.BroadcastFurnanceSmeltStart(furnaceId, (int)objectId, (int)recipe.smeltingTime);

        // 1-1. 호스트 ui 업데이트
        if (FurnaceClientManager.Instance != null && ConnectManager.Instance.isHost)
        {
            FurnaceObject localFurnace = FurnaceClientManager.Instance.GetFurnaceObject(furnaceId);
            if (localFurnace != null)
            {
                // 호스트 눈에 띄게 UI와 파티클 바로 실행!
                localFurnace.OnSmeltStarted((int)recipe.smeltingTime);
            }
        }
        
        Debug.Log("녹이는중...");
        // 2. 정해진 시간 동안 대기
        yield return new WaitForSeconds(recipe.smeltingTime);
        Debug.Log("녹이는 완료!");
        
        // 4. 추적 중인 리스트에서 제거하여 완료 상태로 전환
        activeFurnaces.Remove(furnaceId);
        completedFurnaces.Add(furnaceId, recipe.outputItemID); // 용광로 ID와 완성된 아이템 ID 저장
        // 4-1. 호스트 ui 업데이트 (작업 완료)
        if (FurnaceClientManager.Instance != null && ConnectManager.Instance.isHost)
        {
            FurnaceObject localFurnace = FurnaceClientManager.Instance.GetFurnaceObject(furnaceId);
            Debug.Log($"[FurnaceServerManager] 호스트 UI 업데이트 시도: furnaceId={furnaceId}, furnaceObj={localFurnace != null}");
            if (localFurnace != null)
            {
                localFurnace.OnSmeltCompleted();
                Debug.Log($"[FurnaceServerManager] OnSmeltCompleted 호출 완료, hasResult={localFurnace.hasResult}");
            }
        }

        // 5. 다른 클라이언트들에게 작업 완료 패킷 브로드캐스트
        ItemType resultItemType = (ItemType)recipe.outputItemID;
        PacketSender.Instance.BroadcastFurnanceSmeltComplete((int)objectId, furnaceId, resultItemType);
    }

    // 클라이언트로부터 C_FURNACE_RETRIEVE 패킷 정보를 받았을 때 호출 (어떤 용광로인지 정보가 필요함)
    public void OnReceiveFurnaceRetrieve(int furnaceId) // objectId 매개변수 제거 (꺼내는 요청은 해당 용광로 번호만으로 판단)
    {
        Debug.Log($"[FurnaceServerManager] 수거 요청 수신: furnaceId={furnaceId}, activeFurnaces={activeFurnaces.ContainsKey(furnaceId)}, completedFurnaces={completedFurnaces.ContainsKey(furnaceId)}");

        if (activeFurnaces.ContainsKey(furnaceId))
        {
            Debug.LogWarning($"[Server] 용광로({furnaceId})는 아직 제련 중입니다.");
            return;
        }

        if (completedFurnaces.TryGetValue(furnaceId, out int resultItemId))
        {
            completedFurnaces.Remove(furnaceId);
            ItemType itemResult = (ItemType)resultItemId;
            PacketSender.Instance.BroadcastFurnaceRetrieve(furnaceId, itemResult);

            if (ConnectManager.Instance.isHost && FurnaceClientManager.Instance != null)
            {
                Debug.Log($"[FurnaceServerManager] 호스트 로컬 아이템 생성: furnaceId={furnaceId}, resultItemId={resultItemId}");
                FurnaceClientManager.Instance.SpawnResultItemLocal(furnaceId, resultItemId);
            }
        }
        else
        {
            Debug.LogWarning($"[Server] 용광로({furnaceId})에는 수거할 완성품이 없습니다.");
        }
    }
}