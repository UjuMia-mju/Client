using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Main 씬 ESC: MenuPanel 서브 패널이 열려 있으면 닫기만, 그 외(인트로·메뉴 초기)에는 ExitPopup.
/// </summary>
public class MainExitInputController : MonoBehaviour
{
    [SerializeField] MenuManager menuManager;

    void Awake()
    {
        if (menuManager == null)
            menuManager = FindFirstObjectByType<MenuManager>();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != Define.Scene.MAIN)
            return;

        if (InputManager.IsEscBlockedForHud)
            return;

        if (Keyboard.current == null || !Keyboard.current[Key.Escape].wasPressedThisFrame)
            return;

        if (ExitPopupManager.IsOpen)
        {
            ExitPopupManager.Hide();
            return;
        }

        if (menuManager != null && menuManager.TryCloseSubPanelFromEscape())
            return;

        ExitPopupManager.ShowQuitConfirm();
    }
}
