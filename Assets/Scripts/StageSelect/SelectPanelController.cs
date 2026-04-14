using UnityEngine;
using TMPro;

public class SelectPanelController : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI playTimeText;   
    public TextMeshProUGUI rightText;
    
    public void SetInfo(string stageName, int difficulty, string description, int estimatedClearTimeSeconds)
    {
        if (titleText != null) titleText.text = stageName;
        
        if (difficultyText != null) 
            difficultyText.text = $"난이도 : {difficulty}"; 

        if (playTimeText != null) 
            playTimeText.text = $"예상 소요 시간 : {FormatTime(estimatedClearTimeSeconds)}";
            
        if (rightText != null) 
            rightText.text = description;
    }

    private static string FormatTime(int totalSeconds)
    {
        if (totalSeconds <= 0)
            return "미정";

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }
}