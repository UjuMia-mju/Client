using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

        if (IsHostNow())
            _timerCoroutine = StartCoroutine(StartTimer());
    }

    private static bool IsHostNow()
        => ConnectManager.Instance != null && ConnectManager.Instance.isHost;

    IEnumerator StartTimer()
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);

            // 매 틱마다 재확인: 도중에 역할이 피어로 바뀌었다면 즉시 중단
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
        // 피어 전용. 호스트가 자신의 echo로 SyncTimer를 받지 않도록 방어.
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
            panel.ConfigureNavigation(GoStageSelectAfterPanel, ReloadCurrentStageAfterPanel);
            panel.PlayRevealSequence(data, filledStarCount);
            return;
        }

        Time.timeScale = 1f;
        Debug.Log("ClearPanel 없음 — 스테이지 선택으로 즉시 이동합니다.");
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        else
            SceneManager.LoadScene(Define.Scene.STAGE_SELECT);
    }

    private void GoStageSelectAfterPanel()
    {
        Time.timeScale = 1f;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        else
            SceneManager.LoadScene(Define.Scene.STAGE_SELECT);
    }

    private void ReloadCurrentStageAfterPanel()
    {
        Time.timeScale = 1f;
        var name = SceneManager.GetActiveScene().name;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(name);
        else
            SceneManager.LoadScene(name);
    }
}