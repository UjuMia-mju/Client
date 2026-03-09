using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectPanelController : MonoBehaviour
{
    [Header("UI Settings")] 
    [SerializeField] private TextMeshProUGUI chapterText;
    [SerializeField] private TextMeshProUGUI leftText;
    [SerializeField] private TextMeshProUGUI rightText;

    void Start()
    {
        // TODO: 서버에서 받아온 데이터 새로고침
    }
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("Scenes/GameStages/Stage01Level01");
    }
}
