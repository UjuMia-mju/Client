using UnityEngine;
using System.Collections;

public class GameRuleManager : MonoBehaviour
{
    private float timerDuration = 10f;
    private float remainingTime;

    void Start()
    {
        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            Debug.Log("남은 시간: " + remainingTime + "초");
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
}
