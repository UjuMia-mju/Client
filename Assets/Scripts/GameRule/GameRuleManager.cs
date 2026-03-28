using UnityEngine;
using System.Collections;

public class GameRuleManager : MonoBehaviour
{
    public static GameRuleManager Instance { get; private set; }

    [SerializeField]
    private float timerDuration;  // 제한시간
    private float remainingTime;
    public float GetRemainingTime() => remainingTime;    // 현재 남은 시간
    private bool isVictory = false; // 승패여부
    private bool isGameDone = false;    // 게임이 종료된 것이 맞는지

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f); // 1초마다 감소
            remainingTime -= 1f;
        }

        Debug.Log("타이머 종료!");
        OnTimerEnd();
    }

    void OnTimerEnd()
    {
        // 타이머가 끝났을 때 실행할 로직
        Debug.Log("타이머가 종료되어 게임이 실패로 끝났습니다.");
        ReturnToStageSelectScene(false);
    }

    public void ReturnToStageSelectScene(bool data)
    {
        if (isGameDone)
        {
            return;
        }

        isVictory = data;

        Time.timeScale = 0f; // 게임 일시정지
        isGameDone = true;

        Debug.Log("게임 종료! 승리 여부: " + isVictory);

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(Define.Scene.STAGE_SELECT);
        }
        else
        {
            Debug.LogWarning("SceneLoader가 없습니다. 씬을 단독으로 실행해 테스트중일 가능성이 있습니다.");
        }
    }
}