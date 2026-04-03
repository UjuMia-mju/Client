using TMPro;
using UnityEngine;

/// <summary>
/// 게임 플레이 시간을 서버에서 받아와서 UI에 반영
/// </summary>
public class TimerController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    void Update()
    {
        timerText.text = ConvertServerTimeToString();
    }
    
    // TODO: 서버에서 currentTime을 불러오는 함수
    private string ConvertServerTimeToString()
    {
        if (GameRuleManager.Instance == null)
            return "00:00";

        float remaining = GameRuleManager.Instance.GetRemainingTime();

        // 음수 방지
        remaining = Mathf.Max(0f, remaining);

        int totalSeconds = Mathf.CeilToInt(remaining);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{minutes:D2}:{seconds:D2}";
    }
}