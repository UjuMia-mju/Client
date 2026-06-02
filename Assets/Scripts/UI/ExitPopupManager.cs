using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 종료 확인 팝업(ExitPopup) 표시. <see cref="SceneLoader"/> Awake에서 프리팹을 등록합니다.
/// </summary>
public static class ExitPopupManager
{
    /// <summary>로딩(15)·페이드(10) 등보다 항상 위에 표시합니다.</summary>
    public const int CanvasSortOrder = 32760;

    static GameObject _prefab;
    static ExitPopupController _popup;

    public static bool IsOpen => _popup != null && _popup.IsVisible;

    public static void Initialize(GameObject exitPopupPrefab)
    {
        _prefab = exitPopupPrefab;
    }

    public static void ShowQuitConfirm()
    {
        if (!EnsurePopup())
            return;

        _popup.ShowAnimated();
    }

    public static void Hide()
    {
        _popup?.HideAnimated();
    }

    public static void ToggleQuitConfirm()
    {
        if (IsOpen)
            Hide();
        else
            ShowQuitConfirm();
    }

    /// <summary>ExitPopup '예' — 방 정리 후 애플리케이션 종료.</summary>
    public static void ConfirmQuitApplication()
    {
        if (NetManager.Instance != null && NetManager.Instance.IsConnected)
        {
            if (IsStageSelectMultiplayerHost())
                StageManager.NotifyHostEndingStageSessionForAllPeers();

            PacketDispatcher.Instance?.SendLeaveRoom();
        }

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static bool IsStageSelectMultiplayerHost()
    {
        if (SceneManager.GetActiveScene().name != Define.Scene.STAGE_SELECT)
            return false;

        var tracker = RoomMembershipTracker.Instance;
        if (tracker == null)
            return false;

        tracker.EnsureWired();
        return tracker.OrderedIds.Count > 0 && tracker.AmIFirst();
    }

    static bool EnsurePopup()
    {
        if (_popup != null)
            return true;

        if (_prefab == null)
        {
            Debug.LogError("[ExitPopupManager] exitPopupPrefab이 비어 있습니다. SceneLoader에 ExitPopup 프리팹을 연결하세요.");
            return false;
        }

        var instance = Object.Instantiate(_prefab);
        instance.name = "ExitPopup (Runtime)";
        Object.DontDestroyOnLoad(instance);

        _popup = instance.GetComponent<ExitPopupController>();
        if (_popup == null)
            _popup = instance.AddComponent<ExitPopupController>();

        _popup.InitializeCanvasSortOrder(CanvasSortOrder);
        instance.SetActive(false);
        return true;
    }
}
