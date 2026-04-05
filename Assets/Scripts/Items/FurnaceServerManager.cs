using System.Collections;
using System.Collections.Generic;
using Protocol;
using UnityEngine;

// 서버에서 존재하는 모든 용광로의 작업을 총괄하는 매니저
public class FurnaceServerManager : MonoBehaviorSingleton<FurnaceServerManager>
{
    // 용광로 ID(furnaceId)를 키로 하여 현재 진행 중인 제련 코루틴을 추적
    private Dictionary<int, Coroutine> activeFurnaces = new ();

    // 클라이언트로부터 C_OBJECT_SMELT 패킷을 받았을 때 호출 (어떤 용광로인지 정보가 필요함)
    public void OnReceiveSmeltRequest(int furnaceId, ulong objectId)
    {
        // 1. 해당 용광로가 이미 작동 중인지 확인
        if (activeFurnaces.ContainsKey(furnaceId))
        {
            Debug.LogWarning($"[Server] 용광로({furnaceId})는 이미 작동 중입니다.");
            return;
        }

        int inputItemId = 1; // 추후 이 아이템이 어떤건지 확인하는 로직 필요.

        // 2. 레시피 확인
        if (SmeltingRecipeManager.Instance.TryGetRecipe(inputItemId, out SmeltingRecipe recipe))
        {
            // 중앙 매니저에서 코루틴 시작 후 딕셔너리에 등록
            Coroutine routine = StartCoroutine(SmeltingRoutine(furnaceId, objectId, recipe));
            activeFurnaces.Add(furnaceId, routine);
            Debug.Log($"[Server] 용광로({furnaceId})에서 제련 시작: 아이템 {inputItemId} -> 결과 {recipe.outputItemID}, 소요 시간 {recipe.smeltingTime}초");
        }
        else
        {
            Debug.LogWarning($"[Server] 아이템({inputItemId})은 녹일 수 없습니다.");
        }
    }

    private IEnumerator SmeltingRoutine(int furnaceId, ulong objectId, SmeltingRecipe recipe)
    {
        // 1. 다른 클라이언트들에게 작업 시작 패킷 브로드캐스트
        PacketSender.Instance.BroadcastFurnanceSmeltStart(furnaceId, (int)objectId, (int)recipe.smeltingTime);
        Debug.Log("녹이는중...");
        // 2. 정해진 시간 동안 대기
        yield return new WaitForSeconds(recipe.smeltingTime);
        Debug.Log("녹이는 완료!");
        // 3. 작업 완료 처리 (Item 제거 및 결과 아이템 생성 등, UI 업데이트 등) -> 이 부분은 클라분들이 해주세요
        

        // 4. 추적 중인 리스트에서 제거하여 완료 상태로 전환
        activeFurnaces.Remove(furnaceId);

        // 5. 다른 클라이언트들에게 작업 완료 패킷 브로드캐스트
        ItemType resultItemType = (ItemType)recipe.outputItemID;
        PacketSender.Instance.BroadcastFurnanceSmeltComplete((int)objectId, furnaceId, resultItemType);
    }
}