using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectPanelController : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI chapterTextUI;
    public TextMeshProUGUI leftTextUI;
    public TextMeshProUGUI rightTextUI;
    
    public void SetInfo(string chapter, string leftText, string rightText)
    {
        if (chapterTextUI != null) chapterTextUI.text = chapter;
        if (leftTextUI != null) leftTextUI.text = leftText;
        if (rightTextUI != null) rightTextUI.text = rightText;
    }
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("Scenes/GameStages/Stage01Level01");
    }
}
