/// <summary>
/// 스테이지 선택 멀티 로비 — 서버 측 구현·QA용 체크리스트.
/// 서버 배포 후 검증이 끝나면 <see cref="ServerRoomLeaveBroadcastsVerified"/> 만 true 로 바꾸면 됩니다(런타임 동작 스위치는 아님).
/// 서버는 퇴장 브로드캐스트를 S_ROOM_MEMBER_LEAVE 로 통일했으며(S_PLAYER_LEAVE 제거), 클라도 해당 패킷만 처리합니다.
/// </summary>
public static class StageSelectLobbyServerContract
{
    /*
    TODO(Server) QA 체크리스트 — 전부 만족 후 ServerRoomLeaveBroadcastsVerified = true

    [ ] 방장이 C_LEAVE_ROOM 보내면, 남은 클라이언트가
        S_ROOM_MEMBER_LEAVE(방장 퇴장) 등으로 “방이 끝났다”고 알 수 있을 것.
        (필요 시 잔여 멤버 각각에게 S_LEAVE_ROOM.)

    [ ] 스테이지 대기실 정책: 방장만 나가도 방 해산이면
        잔여 멤버에게 S_ROOM_MEMBER_LEAVE(퇴장자=방장) 등이 브로드캐스트될 것.
        (new_owner_id 필드 없음 — 방장 교체 없이 해산만.)

    [ ] 위와 맞춰 RoomMembershipTracker: 방장(0번) 퇴장이면 GoMainIfInGame().

    [ ] 멤버 퇴장 시 S_ROOM_MEMBER_LEAVE.player_name 채울 것(퇴장 표시 UI).

    [ ] C_LEAVE_ROOM 없이 연결 끊김 시에도 퇴장·잔여 멤버 알림 처리.
    */

    /// <summary>위 QA를 끝냈으면 true. (에디터에서 미완료 시 한 줄 로그만 사용)</summary>
    public const bool ServerRoomLeaveBroadcastsVerified = false;
}
