using UnityEngine;
using TMPro;

public class SelectPanelController : MonoBehaviour
{
    private const int MaxStars = 3;

    [Header("UI Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI playTimeText;   
    public TextMeshProUGUI rightText;
    
    [Header("Clear stars (서버 Star 1~3, 인덱스 0=1성)")]
    [Tooltip("각 슬롯에서 별 획득 시 켤 오브젝트 (길이 3 권장)")]
    public GameObject[] clearStarObjects;
    [Tooltip("각 슬롯 빈 별(배경) — 항상 켜 둠. clear가 그 위에 순서대로 켜짐")]
    public GameObject[] unclearStarObjects;

    [Header("Clear Status UI (별 배열 미사용 시)")]
    [Tooltip("클리어 했을 때 띄울 이미지 (예: CLEAR 도장)")]
    public GameObject clearImage;
    [Tooltip("클리어하지 않았을 때 띄울 이미지 (예: 자물쇠 그림, 혹은 비워둬도 됨)")]
    public GameObject unclearImage;
    
    public void SetInfo(string stageName, int difficulty, string description, int estimatedClearTimeSeconds, int clearStarCount)
    {
        if (titleText != null) titleText.text = stageName;
        
        if (difficultyText != null) 
            difficultyText.text = $"난이도 : {difficulty}"; 

        if (playTimeText != null) 
            playTimeText.text = $"예상 소요 시간 : {FormatTime(estimatedClearTimeSeconds)}";
            
        if (rightText != null) 
            rightText.text = description;

        clearStarCount = Mathf.Clamp(clearStarCount, 0, MaxStars);

        if (HasStarListsConfigured())
            ApplyStarSlots(clearStarCount);
        else
            ApplyLegacyClearImages(clearStarCount > 0);
    }

    private bool HasStarListsConfigured()
    {
        return clearStarObjects != null && unclearStarObjects != null
               && clearStarObjects.Length > 0 && unclearStarObjects.Length > 0;
    }

    private void ApplyStarSlots(int clearStarCount)
    {
        // unclear / clear 배열 길이가 달라도, 배열에 들어 있는 unclear는 전부 켠다.
        if (unclearStarObjects != null)
        {
            for (int i = 0; i < unclearStarObjects.Length; i++)
            {
                if (unclearStarObjects[i] != null)
                    unclearStarObjects[i].SetActive(true);
            }
        }

        if (clearStarObjects != null)
        {
            int n = Mathf.Min(MaxStars, clearStarObjects.Length);
            for (int i = 0; i < n; i++)
            {
                if (clearStarObjects[i] != null)
                    clearStarObjects[i].SetActive(i < clearStarCount);
            }
        }

        // 레거시 단일 이미지는 끄되, 별 슬롯과 같은 레퍼런스면 건드리지 않음(꺼지는 현상 방지)
        if (clearImage != null && !IsInArray(clearImage, clearStarObjects) && !IsInArray(clearImage, unclearStarObjects))
            clearImage.SetActive(false);
        if (unclearImage != null && !IsInArray(unclearImage, clearStarObjects) && !IsInArray(unclearImage, unclearStarObjects))
            unclearImage.SetActive(false);
    }

    private static bool IsInArray(GameObject target, GameObject[] array)
    {
        if (target == null || array == null) return false;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == target) return true;
        }

        return false;
    }

    private void ApplyLegacyClearImages(bool isCleared)
    {
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
    
    public void OnClickPlayButton()
    {
        StageManager.Instance.EnterSelectedStage();
    }
}