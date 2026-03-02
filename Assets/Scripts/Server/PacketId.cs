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

        // 애니메이션 처리를 위해 새롭게 넣었습니다. 20260301
        PKT_C_ANIMATION = 1011,
        PKT_S_ANIMATION = 1012,
    }
}
