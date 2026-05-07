using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Protocol;

public class GameRuleManager : MonoBehaviour
{
    public static GameRuleManager Instance { get; private set; }

    [SerializeField] private float timerDuration;
    [SerializeField] private ClearPanelController clearPanel;
    private float remainingTime;
    public float GetRemainingTime() => remainingTime;
    private bool isVictory = false;
    private bool isGameDone = false;

    private const float TIMER_SYNC_INTERVAL = 1f;
    private float _lastSyncTime = 0f;

    private Coroutine _timerCoroutine;

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[GameRuleManager] Start. ConnectManager.isHost={ConnectManager.Instance?.isHost}");

        // [추가] 게임 씬에서 호스트 다시하기 시 서버 응답 S_GAME_READY_TO_START를 받아 현재 씬 재로드.
        // 호스트 나가기 시 피어가 받는 S_RETURN_TO_STAGE_SELECT도 여기서 처리.
        // [수정] OnGameReadyToStartEvent는 데디 서버 발신 → PacketHandler에 있음.
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnGameReadyToStartEvent += OnGameReadyToStartInGame;

        if (HostPacketHandler.Instance != null)
            HostPacketHandler.Instance.OnReturnToStageSelectEvent += OnReturnToStageSelectFromHost;

        if (IsHostNow())
            _timerCoroutine = StartCoroutine(StartTimer());
    }

    private void OnDestroy()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnGameReadyToStartEvent -= OnGameReadyToStartInGame;

        if (HostPacketHandler.Instance != null)
            HostPacketHandler.Instance.OnReturnToStageSelectEvent -= OnReturnToStageSelectFromHost;
    }

    private static bool IsHostNow()
        => ConnectManager.Instance != null && ConnectManager.Instance.isHost;

    IEnumerator StartTimer()
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);

            if (!IsHostNow())
            {
                Debug.LogWarning("[GameRuleManager] 호스트 권한 상실, 타이머 코루틴 중단");
                _timerCoroutine = null;
                yield break;
            }

            remainingTime -= 1f;

            if (Time.time - _lastSyncTime >= TIMER_SYNC_INTERVAL)
            {
                PacketSender.Instance.BroadcastTimerSync(remainingTime);
                _lastSyncTime = Time.time;
            }
        }

        Debug.Log("타이머 종료!");
        _timerCoroutine = null;
        OnTimerEnd();
    }

    void OnTimerEnd()
    {
        Debug.Log("타이머가 종료되어 게임이 실패로 끝났습니다.");

        if (IsHostNow())
            PacketSender.Instance.BroadcastSpaceshipComplete(false);

        ReturnToStageSelectScene(false, 0);
    }

    public void SyncTimer(float time)
    {
        if (IsHostNow()) return;
        remainingTime = time;
    }

    /// <param name="filledStarCount">패널에 표시할 채운 별 개수. 게임 오버면 0을 넘기면 됩니다.</param>
    public void ReturnToStageSelectScene(bool data, int filledStarCount)
    {
        if (isGameDone) return;

        isVictory = data;
        isGameDone = true;
        Time.timeScale = 0f;

        Debug.Log($"게임 종료! 승리 여부: {isVictory}, 별: {filledStarCount}");

        var panel = clearPanel != null
            ? clearPanel
            : FindFirstObjectByType<ClearPanelController>(FindObjectsInactive.Include);

        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            // [수정] 다시하기 콜백도 함께 등록. 두 콜백 모두 내부에서 호스트 권한 체크.
            panel.ConfigureNavigation(OnExitClicked, OnReplayClicked);
            panel.PlayRevealSequence(data, filledStarCount);
            return;
        }

        Time.timeScale = 1f;
        Debug.Log("ClearPanel 없음 — 스테이지 선택으로 즉시 이동합니다.");
        GoStageSelectLocal();
    }

    // ============================================================
    // [추가] ClearPanel 버튼 콜백
    // ============================================================

    /// <summary>나가기 버튼. 호스트만 의미를 가짐. 피어는 클릭해도 무시.</summary>
    private void OnExitClicked()
    {
        if (!IsHostNow())
        {
            Debug.Log("[GameRuleManager] 피어는 나가기 버튼 무시. 호스트의 결정을 기다리세요.");
            return;
        }

        // 호스트: 모든 피어에게 신호 → 자기 자신도 즉시 이동
        PacketSender.Instance.BroadcastReturnToStageSelect();
        GoStageSelectLocal();
    }

    /// <summary>다시하기 버튼. 호스트만 의미를 가짐. 피어는 클릭해도 무시.</summary>
    private void OnReplayClicked()
    {
        if (!IsHostNow())
        {
            Debug.Log("[GameRuleManager] 피어는 다시하기 버튼 무시. 호스트의 결정을 기다리세요.");
            return;
        }

        int mapId = StageManager.LastLoadedMapId;
        int chapter = StageManager.LastLoadedChapter;
        int stage = StageManager.LastLoadedStageNum;

        if (mapId == 0)
        {
            Debug.LogWarning("[GameRuleManager] LastLoadedMapId가 0. 다시하기 컨텍스트 없음.");
            return;
        }

        Debug.Log($"[GameRuleManager] 다시하기 요청: MapId={mapId}, Chapter={chapter}, Stage={stage}");
        // 서버가 응답으로 S_GAME_READY_TO_START를 모두에게 송신 → 양쪽 클라이언트가
        // OnGameReadyToStartInGame에서 현재 씬을 재로드.
        PacketDispatcher.Instance.SendStartStage(mapId, chapter, stage);
    }

    // ============================================================
    // [추가] 호스트로부터 수신
    // ============================================================

    /// <summary>다시하기 응답: 서버가 보낸 S_GAME_READY_TO_START 수신 → 현재 씬 재로드.</summary>
    private void OnGameReadyToStartInGame(S_GAME_READY_TO_START packet)
    {
        bool isHost = packet.IdOrder.Count > 0
                      && packet.IdOrder[0] == (ulong)NetManager.Instance._playerId;
        ConnectManager.Instance.SetHostRole(isHost);

        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameRuleManager] 다시하기 수신 → 현재 씬 재로드: {scene}");

        Time.timeScale = 1f;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(scene);
        else
            SceneManager.LoadScene(scene);
    }

    /// <summary>피어 측: 호스트의 나가기 신호 수신 → 스테이지 선택으로 이동.</summary>
    private void OnReturnToStageSelectFromHost(S_RETURN_TO_STAGE_SELECT _)
    {
        Debug.Log("[GameRuleManager] 호스트 나가기 신호 수신 → StageSelect로 이동");
        GoStageSelectLocal();
    }

    private void GoStageSelectLocal()
    {
        Time.timeScale = 1f;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        else
            SceneManager.LoadScene(Define.Scene.STAGE_SELECT);
    }
}