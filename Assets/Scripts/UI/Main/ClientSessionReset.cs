/// <summary>
/// 메인·인게임 복귀 시 게임 세션 정리. TCP(ConnectManager·NetManager)는 유지합니다.
/// </summary>
public static class ClientSessionReset
{
    public static bool ShouldTryLeaveRoom()
    {
        var nm = NetManager.Instance;
        return nm != null && nm.IsConnected && nm._playerId != 0;
    }

    /// <summary>방·계정 세션·공용 캐시(스테이지/방/게이트 등) 초기화. Disconnect 호출 없음.</summary>
    public static void ClearLocalSessionState()
    {
        SinglePlaySession.End();

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.HideLoadingOverlay();

        PacketHandler.ClearCachedEnterRoom();
        RoomMembershipTracker.Instance?.Reset();
        GameplayReadyCoordinator.ResetForLogout();
        ReadyToStartPanelController.DismissAllActive();
        DbCacheManager.ClearStageCache();

        if (ConnectManager.Instance != null)
            ConnectManager.Instance.SetHostRole(false);

        NetManager.Instance?.ClearLocalPlayerSession();
    }
}
