using UnityEngine;
using TMPro;

public class SelectPanelController : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI playTimeText;   
    public TextMeshProUGUI rightText;
    
    [Header("Clear Status UI")]
    [Tooltip("클리어 했을 때 띄울 이미지 (예: CLEAR 도장)")]
    public GameObject clearImage;
    [Tooltip("클리어하지 않았을 때 띄울 이미지 (예: 자물쇠 그림, 혹은 비워둬도 됨)")]
    public GameObject unclearImage;
    
    public void SetInfo(string stageName, int difficulty, string description, int estimatedClearTimeSeconds, bool isCleared)
    {
        if (titleText != null) titleText.text = stageName;
        
        if (difficultyText != null) 
            difficultyText.text = $"난이도 : {difficulty}"; 

        if (playTimeText != null) 
            playTimeText.text = $"예상 소요 시간 : {FormatTime(estimatedClearTimeSeconds)}";
            
        if (rightText != null) 
            rightText.text = description;

        // 클리어 상태에 따라 이미지를 껐다 켰다 하는 로직
        if (clearImage != null) clearImage.SetActive(isCleared);
        if (unclearImage != null) unclearImage.SetActive(!isCleared);
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