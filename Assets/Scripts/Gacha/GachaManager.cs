using System.Collections.Generic;
using UnityEngine;
using Protocol;

public class GachaManager : MonoBehaviour
{
    // 서버 결과를 UI 아이콘/이름과 매칭할 때 사용하는 로컬 마스터 데이터
    public List<GachaItem> allItems;

    // 획득한 아이템 목록
    private List<GachaItem> obtainedItems;
    private readonly List<GachaPoolInfo> serverPools = new List<GachaPoolInfo>();

    [Header("Server Gacha Settings")]
    public int selectedPoolId = 1;
    public int defaultPullCount = 1;

    [Header("UI Connection")]
    public GachaSpinnerUI spinnerUI; // 인스펙터에서 연결
    public GachaResultPopupUI resultPopupUI; // 스핀 종료 후 결과 표시

    void Start()
    {
        // dedicate 서버 수신 이벤트 구독
        PacketHandler.Instance.OnGachaPoolListEvent += OnGachaPoolList;
        PacketHandler.Instance.OnGachaResultEvent += OnGachaResult;
        PacketHandler.Instance.OnMySkinsEvent += OnMySkins;
        if (spinnerUI != null)
            spinnerUI.OnSpinFinished += OnSpinFinished;

        // 게임 시작 시 초기화(테스트용으로 자동 리셋)
        ResetGacha();

        // 테스트 편의를 위해 진입 즉시 기본 데이터 요청
        PacketDispatcher.Instance.SendGachaPoolList();
        PacketDispatcher.Instance.SendMySkins();
    }

    void OnDestroy()
    {
        if (PacketHandler.Instance == null)
            return;

        // 씬 전환/오브젝트 제거 시 중복 수신 방지
        PacketHandler.Instance.OnGachaPoolListEvent -= OnGachaPoolList;
        PacketHandler.Instance.OnGachaResultEvent -= OnGachaResult;
        PacketHandler.Instance.OnMySkinsEvent -= OnMySkins;
        if (spinnerUI != null)
            spinnerUI.OnSpinFinished -= OnSpinFinished;
    }

    // 가챠 시스템 초기화 (리셋)
    public void ResetGacha()
    {
        obtainedItems = new List<GachaItem>();
        Debug.Log("가챠 시스템이 초기화되었습니다.");
    }

    // 뽑기 요청은 로컬 RNG가 아니라 서버에 위임
    public void PullItem()
    {
        if (defaultPullCount <= 0)
            defaultPullCount = 1;

        if (selectedPoolId <= 0)
        {
            Debug.LogWarning("유효한 가챠 풀 ID가 없습니다. 풀 목록을 먼저 받아오세요.");
            return;
        }

        PacketDispatcher.Instance.SendGacha(selectedPoolId, defaultPullCount);
    }

    public void RequestPoolList()
    {
        PacketDispatcher.Instance.SendGachaPoolList();
    }

    public void RequestMySkins()
    {
        PacketDispatcher.Instance.SendMySkins();
    }

    private void OnGachaPoolList(S_GACHA_POOL_LIST packet)
    {
        // 서버가 내려준 활성 풀 스냅샷으로 교체
        serverPools.Clear();
        foreach (var pool in packet.Pools)
            serverPools.Add(pool);

        if (serverPools.Count > 0 && selectedPoolId <= 0)
            selectedPoolId = serverPools[0].PoolId;

        Debug.Log($"가챠 풀 {serverPools.Count}개 수신. 현재 poolId={selectedPoolId}");
    }

    private void OnGachaResult(S_GACHA packet)
    {
        if (!packet.Success)
        {
            Debug.LogError($"가챠 실패: {packet.ErrorMsg}");
            return;
        }

        if (packet.Result == null || packet.Result.ObtainedSkins.Count == 0)
        {
            Debug.LogWarning("가챠 결과에 획득 스킨이 없습니다.");
            return;
        }

        // 현재 스피너 UI가 단일 결과 연출이라 첫 번째 스킨 기준으로 표시
        var firstSkin = packet.Result.ObtainedSkins[0];
        var selectedItem = FindItemBySkin(firstSkin);
        if (selectedItem == null)
        {
            Debug.LogWarning($"매칭되는 로컬 아이템이 없어 연출을 생략합니다. skin={firstSkin.SkinName}");
            return;
        }

        obtainedItems.Add(selectedItem);
        spinnerUI.StartSpinAnimation(selectedItem);

        Debug.Log($"가챠 성공: {packet.Result.ObtainedSkins.Count}개, gems={packet.Result.RemainingGems}, coins={packet.Result.RemainingCoins}");
    }

    private void OnMySkins(S_MY_SKINS packet)
    {
        Debug.Log($"보유 스킨 {packet.Skins.Count}개 수신");
    }

    private void OnSpinFinished(GachaItem item)
    {
        if (resultPopupUI != null)
            resultPopupUI.Show(item);
    }

    private GachaItem FindItemBySkin(SkinInfo skin)
    {
        if (allItems == null)
            return null;

        // 서버 스킨명과 로컬 에셋명을 1:1 매칭
        foreach (var item in allItems)
        {
            if (item != null && item.itemName == skin.SkinName)
                return item;
        }

        return null;
    }
}