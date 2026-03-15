using UnityEngine;
using TMPro;

public class SelectPanelController : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI playTimeText;   
    public TextMeshProUGUI rightText;
    
    public void SetInfo(string stageName, int difficulty, string description)
    {
        if (titleText != null) titleText.text = stageName;
        
        if (difficultyText != null) 
            difficultyText.text = $"난이도 : {difficulty}"; 

        // TODO: 패킷 추가되면 적용
        if (playTimeText != null) 
            playTimeText.text = $"예상 소요 시간 : 미정";
            
        if (rightText != null) 
            rightText.text = description;
    }
}