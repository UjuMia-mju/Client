/// <summary>
/// 메인 메뉴 싱글플레이: 멀티플레이와 동일하게 방 생성·시작 후 스테이지 선택으로 진입하는 세션 플래그.
/// </summary>
public static class SinglePlaySession
{
    /// <summary>싱글플레이 버튼으로 진입한 세션(스테이지 선택·인게임 포함).</summary>
    public static bool IsActive { get; private set; }

    /// <summary>방 생성~S_START_ROOM 성공 전. MainMultiPlayHandler가 로비 대신 스테이지 선택으로 보냄.</summary>
    public static bool IsAwaitingRoomBootstrap { get; private set; }

    public static void BeginSoloMultiplayer()
    {
        End();
        IsActive = true;
        IsAwaitingRoomBootstrap = true;
    }

    public static void OnRoomBootstrapComplete()
    {
        IsAwaitingRoomBootstrap = false;
    }

    public static void End()
    {
        if (!IsActive && !IsAwaitingRoomBootstrap) return;
        IsActive = false;
        IsAwaitingRoomBootstrap = false;
        UnityEngine.Debug.Log("[SinglePlaySession] 싱글플레이 세션 종료");
    }
}
