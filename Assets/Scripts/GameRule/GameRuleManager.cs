using UnityEngine;
using System.Collections;

public class GameRuleManager : MonoBehaviour
{
    public static GameRuleManager Instance { get; private set; }

    [SerializeField] private float timerDuration;
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

        ReturnToStageSelectScene(false);
    }

    public void SyncTimer(float time)
    {
        // 피어 전용. 호스트가 자신의 echo로 SyncTimer를 받지 않도록 방어.
        if (IsHostNow()) return;

        remainingTime = time;
    }

    public void ReturnToStageSelectScene(bool data)
    {
        if (isGameDone) return;

        isVictory = data;
        isGameDone = true;
        Time.timeScale = 0f;

        Debug.Log($"게임 종료! 승리 여부: {isVictory}");

        if (SceneLoader.Instance != null)
        {
            Time.timeScale = 1f;
            Debug.Log("스테이지 선택 씬으로 이동합니다.");
            SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        }
    }
}