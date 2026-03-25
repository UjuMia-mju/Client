using UnityEngine;
using System.Collections;

public class GameRuleManager : MonoBehaviour
{
    public static GameRuleManager Instance { get; private set; }
    private float timerDuration = 10f;
    private float remainingTime;

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
        Debug.Log("게임 규칙 실행!");
    }

    public float RemainingTime()
    {
        return remainingTime;
    }
}
