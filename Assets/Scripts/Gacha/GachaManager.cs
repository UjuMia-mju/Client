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

        isGachaRequestPending = true;
        PacketDispatcher.Instance.SendGacha(selectedPoolId, defaultPullCount);
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
            Debug.LogError($"가챠 실패: {packet.ErrorMsg}");
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
