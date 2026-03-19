using ParrelSync;
using Unity.VisualScripting;
using UnityEngine;

public class PausePanelController : MonoBehaviour
{
    [SerializeField] private GameObject SettingsPanel;

    public void OnSettingsButtonClicked()
    {
        var hud = Object.FindFirstObjectByType<HUDManager>();
        hud.OpenPanel(SettingsPanel);
    }

    public void OnMainMenuButtonClicked()
    {
        SceneLoader.Instance.LoadScene("Main");
    }

    public void OnExitButtonClicked()
    {
        // 1. 실제 빌드된 게임 종료
        Application.Quit();

        // 2. 유니티 에디터 환경에서 플레이 모드 종료
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}