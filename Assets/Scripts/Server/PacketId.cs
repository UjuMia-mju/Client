namespace Protocol
{
    public enum PacketId : ushort
    {
        PKT_C_LOGIN = 1000,
        PKT_S_LOGIN = 1001,
        PKT_C_ENTER_GAME = 1002,
        PKT_S_ENTER_GAME = 1003,
        PKT_C_CHAT = 1004,
        PKT_S_CHAT = 1005,
        PKT_C_MOVE = 1006,
        PKT_S_MOVE = 1007,
        PKT_S_PLAYER_LIST = 1008,
        PKT_S_PLAYER_ENTER = 1009,
        PKT_S_PLAYER_LEAVE = 1010,

        // 방 관련
        PKT_C_CREATE_ROOM = 1011,
        PKT_S_CREATE_ROOM = 1012,
        PKT_C_ROOM_LIST = 1013,
        PKT_S_ROOM_LIST = 1014,
        PKT_C_ENTER_ROOM = 1015,
        PKT_S_ENTER_ROOM = 1016,
        PKT_C_LEAVE_ROOM = 1017,
        PKT_S_LEAVE_ROOM = 1018,

        // 초대 관련
        PKT_C_INVITE_PLAYER = 1019,
        PKT_S_INVITE_PLAYER = 1020,
        PKT_S_INVITE_NOTIFICATION = 1021,
        PKT_C_INVITE_RESPONSE = 1022,
        PKT_S_INVITE_RESPONSE = 1023,

        // 방 멤버 알림
        PKT_S_ROOM_MEMBER_ENTER = 1024,
        PKT_S_ROOM_MEMBER_LEAVE = 1025,

        // 준비 / 게임 시작
        PKT_C_READY = 1026,
        PKT_S_READY = 1027,
        PKT_C_START_ROOM = 1028,
        PKT_S_START_ROOM = 1029,
    }
}