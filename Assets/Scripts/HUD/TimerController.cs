using TMPro;
using UnityEngine;

/// <summary>
/// 게임 플레이 시간을 서버에서 받아와서 UI에 반영
/// </summary>
public class TimerController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private string currentTime;
    void Update()
    {
        //timerText.text = currentTime;
    }
    
    // TODO: 서버에서 currentTime을 불러오는 함수
}
