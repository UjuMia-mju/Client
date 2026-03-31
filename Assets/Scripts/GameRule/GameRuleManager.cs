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

        // HACK : 현재 MonoBehaviorSingleton.cs에서 어떤 인스턴스가 null이면 자동으로 이를 생성하도록 하는 로직이 있습니다.
        // 해당 부분이 씬을 단독으로 실행시켰을 때 프리팹이 할당되어 있지 않은 문제로 오류를 일으키도록 하고 있으므로, 레벨 디자인 기간동안 이 부분은 주석화하겠습니다.

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