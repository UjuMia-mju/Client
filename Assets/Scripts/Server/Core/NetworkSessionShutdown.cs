#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 플레이 모드 종료·앱 종료 시 TCP 세션을 끊습니다. 에디터 Stop 시 소켓 잔류 방지.
/// </summary>
public static class NetworkSessionShutdown
{
    static bool _shutdownDone;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void RegisterEditorPlayModeExit()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            ShutdownAll();
    }
#endif

    public static void ShutdownAll()
    {
        if (_shutdownDone)
            return;

        _shutdownDone = true;

        NetManager.Shutdown();
        RelayNetManager.Shutdown();
        MainThreadDispatcher.ClearPending();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetShutdownFlag()
    {
        _shutdownDone = false;
    }
}
