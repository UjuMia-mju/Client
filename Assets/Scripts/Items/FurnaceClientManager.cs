using System.Collections.Generic;
using UnityEngine;
using Protocol;

public class FurnaceClientManager : MonoBehaviorSingleton<FurnaceClientManager>
{
    // 씬에 존재하는 용광로 컨트롤러들을 관리하는 딕셔너리
    private Dictionary<int, FurnaceObject> furnaceControllers = new ();
    private const float ITEM_THROW_HEIGHT = 3.5f; 
    private const float ITEM_THROW_FORCE = 200f;

    private void Start()
    {
        // HostPacketHandler의 패킷 수신 이벤트 구독
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent += HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent += HandleSmeltCompleted;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnSmeltEvent -= HandleSmeltStarted;
            HostPacketHandler.Instance.OnSmeltCompleteEvent -= HandleSmeltCompleted;
        }
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
        // 참고: packet 내부 필드명(FurnaceId 등)은 Protocol.proto 정의에 맞게 수정하세요.
        int furnaceId = packet.FurnaceId;

        if (furnaceControllers.TryGetValue(furnaceId, out FurnaceObject furnaceObject))
        {
            // 해당 용광로의 파티클, 사운드 등 작동 종료 연출 (UI적인 처리)
            furnaceObject.OnSmeltCompleted();
            Debug.Log($"[Client] {furnaceId}번 용광로 작동 완료!");
        }
    }
}
