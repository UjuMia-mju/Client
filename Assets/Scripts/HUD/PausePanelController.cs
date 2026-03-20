using ParrelSync;
using Unity.VisualScripting;
using UnityEngine;

public class PausePanelController : MonoBehaviour
{
    [SerializeField] private GameObject SettingsPanel;

    public void OnSettingsButtonClicked()
    {
        var hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud != null)
        {
            // 생성과 동시에 목표 크기를 넘겨줍니다.
            hud.OpenPanel(SettingsPanel, new Vector3(2f, 2f, 1f));
        }
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