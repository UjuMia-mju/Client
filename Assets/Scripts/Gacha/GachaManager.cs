using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Protocol;

public class GachaManager : MonoBehaviour
{
    // 서버 결과를 UI 아이콘/이름과 매칭할 때 사용하는 로컬 마스터 데이터
    public List<GachaItem> allItems;

    private readonly List<GachaPoolInfo> serverPools = new List<GachaPoolInfo>();
    private bool isGachaRequestPending;
    private bool _currencyReceived;
    private int _coin;
    private int _gem;

    [Header("Server Gacha Settings")]
    public int selectedPoolId = 1;
    public int defaultPullCount = 1;

    [Header("UI Connection")]
    public GachaSpinnerUI spinnerUI; // 인스펙터에서 연결
    public GachaResultPopupUI resultPopupUI; // 스핀 종료 후 결과 표시

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private string backSceneName = Define.Scene.MAIN;

    private Coroutine _initialDataRoutine;

    void Start()
    {
        // dedicate 서버 수신 이벤트 구독
        PacketHandler.Instance.OnGachaPoolListEvent += OnGachaPoolList;
        PacketHandler.Instance.OnGachaResultEvent += OnGachaResult;
        PacketHandler.Instance.OnMySkinsEvent += OnMySkins;
        PacketHandler.Instance.OnGetCurrencyEvent += OnGetCurrency;
        if (spinnerUI != null)
            spinnerUI.OnSpinFinished += OnSpinFinished;

        BindBackButton();
        _initialDataRoutine = StartCoroutine(WaitAndRequestInitialData());
    }

    void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);

        if (_initialDataRoutine != null)
        {
            StopCoroutine(_initialDataRoutine);
            _initialDataRoutine = null;
        }

        if (PacketHandler.Instance == null)
            return;

        // 씬 전환/오브젝트 제거 시 중복 수신 방지
        PacketHandler.Instance.OnGachaPoolListEvent -= OnGachaPoolList;
        PacketHandler.Instance.OnGachaResultEvent -= OnGachaResult;
        PacketHandler.Instance.OnMySkinsEvent -= OnMySkins;
        PacketHandler.Instance.OnGetCurrencyEvent -= OnGetCurrency;
        if (spinnerUI != null)
            spinnerUI.OnSpinFinished -= OnSpinFinished;
    }

    IEnumerator WaitAndRequestInitialData()
    {
        const float timeoutSeconds = 20f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            var nm = NetManager.Instance;
            if (nm != null && nm.IsConnected && nm._playerId != 0)
            {
                RequestInitialData();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (NetManager.Instance != null && NetManager.Instance.IsConnected)
            RequestInitialData();
        else
            Debug.LogWarning("[GachaManager] 서버 연결 전이라 가챠 데이터 자동 요청을 건너뜁니다.");
    }

    void RequestInitialData()
    {
        PacketDispatcher.Instance.SendGetCurrency();
        PacketDispatcher.Instance.SendGachaPoolList();
        PacketDispatcher.Instance.SendMySkins();
    }

    private void BindBackButton()
    {
        if (backButton == null)
            return;

        backButton.onClick.AddListener(OnClickBack);
    }

    private void OnClickBack()
    {
        SoundManager.Instance.PlaySFX("Click2");
        SceneLoader.Instance.LoadScene(backSceneName);
    }

    // 뽑기 요청은 로컬 RNG가 아니라 서버에 위임
    public void PullItem()
    {
        if (isGachaRequestPending)
        {
            MessageManager.TryShowKey(MessageKeys.GachaRequestPending);
            return;
        }

        if (spinnerUI != null && spinnerUI.IsSpinning)
        {
            MessageManager.TryShowKey(MessageKeys.GachaSpinInProgress);
            return;
        }

        if (defaultPullCount <= 0)
            defaultPullCount = 1;

        if (selectedPoolId <= 0)
        {
            MessageManager.TryShowKey(MessageKeys.GachaInvalidPool);
            return;
        }

        var nm = NetManager.Instance;
        if (nm == null || !nm.IsConnected)
        {
            MessageManager.TryShowKey(MessageKeys.NotConnected);
            return;
        }

        if (nm._playerId == 0)
        {
            MessageManager.TryShowKey(MessageKeys.MultiplayLoginRequired);
            Debug.LogWarning("[GachaManager] playerId=0 상태입니다. 로그인 후 가챠를 시도하세요.");
            return;
        }

        if (serverPools.Count == 0)
        {
            Debug.LogWarning("[GachaManager] 가챠 풀 목록이 없어 재요청합니다.");
            PacketDispatcher.Instance.SendGachaPoolList();
            MessageManager.TryShowKey(MessageKeys.GachaInvalidPool);
            return;
        }

        if (!TryGetSelectedPool(out GachaPoolInfo pool))
        {
            Debug.LogError(
                $"[GachaManager] poolId={selectedPoolId} 가 서버 풀 목록에 없습니다. " +
                $"수신된 풀 ID: {FormatPoolIds()}");
            MessageManager.TryShowKey(MessageKeys.GachaInvalidPool);
            return;
        }

        // 20260603 홍성민 수정
        // 뽑기 테스트 도중 해당 구문이 문제 없는 풀에도 적용되어 뽑기 패킷을 보내지 않는 로직 에러가 있었습니다.
        // 그런데 이 가드를 추가하신 이유도 아마 있으실 거라고 생각은 하는데, 일단 풀 활성화 여부 자체를 서버쪽에서 검사를 해준다고 합니다. (IsActive)
        // 기능이 동작하지 않으므로 주석화합니다. 혹시 이 여부를 검사하신 이유를 아시는 분이 계시다면 주석으로 서술해주시면 감사하겠습니다.
        //if (!pool.IsActive)
        //{
        //    Debug.LogWarning($"[GachaManager] poolId={pool.PoolId} 비활성 풀입니다.");
        //    MessageManager.TryShowKey(MessageKeys.GachaInvalidPool);
        //    return;
        //}

        if (pool.Skins == null || pool.Skins.Count == 0)
        {
            Debug.LogError(
                $"[GachaManager] poolId={pool.PoolId}({pool.PoolName})에 뽑을 스킨이 0개입니다. 서버 DB/풀 설정을 확인하세요.");
            MessageManager.TryShowKey(MessageKeys.GachaInvalidPool);
            return;
        }

        int pullCount = defaultPullCount <= 0 ? 1 : defaultPullCount;
        if (pool.MaxPull > 0 && pullCount > pool.MaxPull)
        {
            Debug.LogWarning($"[GachaManager] pullCount={pullCount} > maxPull={pool.MaxPull}, maxPull로 제한합니다.");
            pullCount = pool.MaxPull;
            defaultPullCount = pullCount;
        }

        if (_currencyReceived)
        {
            int gemCost = pool.CostGem * pullCount;
            int coinCost = pool.CostCoin * pullCount;
            if (gemCost > 0 && _gem < gemCost)
            {
                Debug.LogWarning(
                    $"[GachaManager] 젬 부족: 보유={_gem}, 필요={gemCost} (poolId={pool.PoolId}, pull={pullCount})");
            }

            if (coinCost > 0 && _coin < coinCost)
            {
                Debug.LogWarning(
                    $"[GachaManager] 코인 부족: 보유={_coin}, 필요={coinCost} (poolId={pool.PoolId}, pull={pullCount})");
            }
        }
        else
        {
            PacketDispatcher.Instance.SendGetCurrency();
        }

        Debug.Log(
            $"[GachaManager] C_GACHA 전송 poolId={selectedPoolId}, pullCount={pullCount}, " +
            $"poolSkins={pool.Skins.Count}, costGem={pool.CostGem}, costCoin={pool.CostCoin}, " +
            $"currency=({(_currencyReceived ? $"{_coin} coin, {_gem} gem" : "미수신")})");

        isGachaRequestPending = true;
        if (!PacketDispatcher.Instance.SendGacha(selectedPoolId, defaultPullCount))
        {
            isGachaRequestPending = false;
            MessageManager.TryShowKey(MessageKeys.NotConnected);
        }
    }

    private void OnGachaPoolList(S_GACHA_POOL_LIST packet)
    {
        // 서버가 내려준 활성 풀 스냅샷으로 교체
        serverPools.Clear();
        foreach (var pool in packet.Pools)
            serverPools.Add(pool);

        if (serverPools.Count > 0 && (selectedPoolId <= 0 || !TryGetSelectedPool(out _)))
            selectedPoolId = serverPools[0].PoolId;

        Debug.Log($"가챠 풀 {serverPools.Count}개 수신. 현재 poolId={selectedPoolId}");
        LogPoolSnapshot();
    }

    private void OnGetCurrency(S_GET_CURRENCY packet)
    {
        _currencyReceived = packet.Success;
        if (!packet.Success)
        {
            Debug.LogWarning("[GachaManager] 재화 조회 실패(S_GET_CURRENCY success=false)");
            return;
        }

        _coin = packet.Coin;
        _gem = packet.Gem;
        Debug.Log($"[GachaManager] 재화 수신 coin={_coin}, gem={_gem}");
    }

    bool TryGetSelectedPool(out GachaPoolInfo pool)
    {
        pool = null;
        foreach (var p in serverPools)
        {
            if (p.PoolId == selectedPoolId)
            {
                pool = p;
                return true;
            }
        }

        return false;
    }

    string FormatPoolIds()
    {
        if (serverPools.Count == 0)
            return "(없음)";

        var ids = new List<int>(serverPools.Count);
        foreach (var p in serverPools)
            ids.Add(p.PoolId);
        return string.Join(", ", ids);
    }

    void LogPoolSnapshot()
    {
        foreach (var pool in serverPools)
        {
            int skinCount = pool.Skins?.Count ?? 0;
            Debug.Log(
                $"[GachaManager] 풀 id={pool.PoolId}, name={pool.PoolName}, active={pool.IsActive}, " +
                $"costGem={pool.CostGem}, costCoin={pool.CostCoin}, maxPull={pool.MaxPull}, skins={skinCount}");
        }
    }

    private SkinInfo _pendingResultSkin;

    private void OnGachaResult(S_GACHA packet)
    {
        isGachaRequestPending = false;

        if (!packet.Success)
        {
            MessageManager.TryShowServerError(
                MessageKeys.GachaFailed,
                MessageKeys.GachaFailedWithReason,
                packet.ErrorMsg);
            Debug.LogError(
                $"가챠 실패(서버 응답): {packet.ErrorMsg} | poolId={selectedPoolId}, pullCount={defaultPullCount}, " +
                $"playerId={NetManager.Instance?._playerId ?? 0}, connected={NetManager.Instance?.IsConnected ?? false}");
            return;
        }

        if (packet.Result == null || packet.Result.ObtainedSkins.Count == 0)
        {
            MessageManager.TryShowKey(MessageKeys.GachaNoResult);
            Debug.LogWarning("가챠 결과에 획득 스킨이 없습니다.");
            return;
        }

        // 현재 스피너 UI가 단일 결과 연출이라 첫 번째 스킨 기준으로 표시
        var firstSkin = packet.Result.ObtainedSkins[0];
        _pendingResultSkin = firstSkin;
        var selectedItem = FindItemBySkin(firstSkin);
        if (selectedItem == null)
        {
            Debug.LogWarning($"매칭되는 로컬 아이템이 없어 연출을 생략합니다. skin={firstSkin.SkinName}");
            return;
        }

        if (spinnerUI == null)
        {
            Debug.LogWarning("Spinner UI가 연결되지 않아 연출을 생략합니다.");
            return;
        }

        if (!spinnerUI.StartSpinAnimation(selectedItem))
        {
            MessageManager.TryShowKey(MessageKeys.GachaSpinStartFailed);
            Debug.LogWarning("스핀 시작에 실패했습니다. 진행 중인 연출이 끝난 뒤 다시 시도하세요.");
            return;
        }

        Debug.Log($"가챠 성공: {packet.Result.ObtainedSkins.Count}개, gems={packet.Result.RemainingGems}, coins={packet.Result.RemainingCoins}");
        PacketDispatcher.Instance.SendMySkins();
    }

    private void OnMySkins(S_MY_SKINS packet)
    {
        Debug.Log($"보유 스킨 {packet.Skins.Count}개 수신");
    }

    private void OnSpinFinished(GachaItem item)
    {
        if (resultPopupUI != null)
            resultPopupUI.Show(item, _pendingResultSkin);
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
