using UnityEngine;
using System.Collections;

public class GameRuleManager : MonoBehaviour
{
    public static GameRuleManager Instance { get; private set; }

    [SerializeField]
    private float timerDuration;
    private float remainingTime;
    public float GetRemainingTime() => remainingTime;
    private bool isVictory = false;
    private bool isGameDone = false;

    private const float TIMER_SYNC_INTERVAL = 1f; // 1초마다 동기화
    private float _lastSyncTime = 0f;

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 호스트만 타이머 실행
        if (ConnectManager.Instance == null || ConnectManager.Instance.isHost)
            StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;

            // 5초마다 피어들에게 타이머 동기화
            if (ConnectManager.Instance != null && Time.time - _lastSyncTime >= TIMER_SYNC_INTERVAL)
            {
                PacketSender.Instance.BroadcastTimerSync(remainingTime);
                _lastSyncTime = Time.time;
            }
        }

        Debug.Log("타이머 종료!");
        OnTimerEnd();
    }

    void OnTimerEnd()
    {
        Debug.Log("타이머가 종료되어 게임이 실패로 끝났습니다.");

        // 호스트가 피어들에게 패배 브로드캐스트
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            PacketSender.Instance.BroadcastSpaceshipComplete(false);

        ReturnToStageSelectScene(false);
    }

    // 피어 전용: 호스트로부터 받은 타이머 동기화
    public void SyncTimer(float time)
    {
        remainingTime = time;
        //Debug.Log($"[GameRuleManager] 타이머 동기화: {remainingTime}초");
    }

    public void ReturnToStageSelectScene(bool data)
    {
        if (isGameDone) return;

        isVictory = data;
        Time.timeScale = 0f;
        isGameDone = true;

        Debug.Log("게임 종료! 승리 여부: " + isVictory);

        //if (SceneLoader.Instance != null)
        //{
        //    Debug.Log("SceneLoader가 있으므로 스테이지 선택 씬으로 돌아갑니다.");
        //    SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        //}
        //else
        //{
        //    Debug.Log("SceneLoader가 없습니다. 씬을 단독으로 실행해 테스트중일 가능성이 있습니다.");
        //}
    }
}