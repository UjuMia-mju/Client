using UnityEngine;
using Protocol;


/// <summary>
///  프로토콜 메시지를 패킷으로 변환하여 NetManager 또는 PeerNetManager를 통해 전송하는 클래스
///  한마디로 서버에 보낼건지, 호스트한테 보낼건지 네트워크 경로를 지정해줌.
/// </summary>
public class PacketDispatcher : Singleton<PacketDispatcher>
{
    private ulong GetLocalPlayerId()
    {
        return (ulong)NetManager.Instance._playerId;
    }
    NetManager net = NetManager.Instance;
    

    #region To Dedicate Server
    public void SendLogin(string userId, string password)
    {
        C_LOGIN loginPacket = new C_LOGIN
        {
            UserId = userId,
            Psw = password
        };

        net.SendPacket(PacketId.PKT_C_LOGIN, loginPacket);
    }

    public void SendGachaPoolList()
    {
        C_GACHA_POOL_LIST packet = new C_GACHA_POOL_LIST();
        net.SendPacket(PacketId.PKT_C_GACHA_POOL_LIST, packet);
    }

    public void SendGacha(int poolId, int pullCount)
    {
        C_GACHA packet = new C_GACHA
        {
            PoolId = poolId,
            PullCount = pullCount
        };
        net.SendPacket(PacketId.PKT_C_GACHA, packet);
    }

    public void SendMySkins()
    {
        C_MY_SKINS packet = new C_MY_SKINS();
        net.SendPacket(PacketId.PKT_C_MY_SKINS, packet);
    }

    // ==================== Lobby/Room ====================

    public void SendCreateRoom()
    {
        C_CREATE_ROOM packet = new C_CREATE_ROOM();
        net.SendPacket(PacketId.PKT_C_CREATE_ROOM, packet);
    }

    public void SendRoomList()
    {
        C_ROOM_LIST packet = new C_ROOM_LIST();
        net.SendPacket(PacketId.PKT_C_ROOM_LIST, packet);
    }

    public void SendEnterRoom(ulong roomId)
    {
        C_ENTER_ROOM packet = new C_ENTER_ROOM
        {
            RoomId = roomId
        };
        net.SendPacket(PacketId.PKT_C_ENTER_ROOM, packet);
    }

    public void SendLeaveRoom()
    {
        C_LEAVE_ROOM packet = new C_LEAVE_ROOM();
        net.SendPacket(PacketId.PKT_C_LEAVE_ROOM, packet);
    }

    public void SendReady(bool isReady)
    {
        C_READY packet = new C_READY
        {
            IsReady = isReady
        };
        net.SendPacket(PacketId.PKT_C_READY, packet);
    }

    public void SendStartRoom()
    {
        C_START_ROOM packet = new C_START_ROOM();
        net.SendPacket(PacketId.PKT_C_START_ROOM, packet);
    }

    /// <summary>
    /// 특정 유저를 방으로 초대한다. player_name + player_tag로 대상 식별.
    /// </summary>
    /// <param name="playerName">초대할 유저의 이름</param>
    /// <param name="playerTag">초대할 유저의 태그 (고유 번호, 예: 1234)</param>
    public void SendInvitePlayer(string playerName, int playerTag)
    {
        C_INVITE_PLAYER invitePlayerPacket = new C_INVITE_PLAYER
        {
            PlayerName = playerName,
            PlayerTag = playerTag
        };

        net.SendPacket(PacketId.PKT_C_INVITE_PLAYER, invitePlayerPacket);
    }

    /// <summary>
    /// 받은 초대에 대해 수락/거절 응답을 보낸다.
    /// </summary>
    /// <param name="inviteId">S_INVITE_NOTIFICATION으로 받은 invite_id</param>
    /// <param name="accept">true: 수락, false: 거절</param>
    public void SendInviteResponse(ulong inviteId, bool accept)
    {
        C_INVITE_RESPONSE inviteResponsePacket = new C_INVITE_RESPONSE
        {
            InviteId = inviteId,
            Accept = accept
        };

        net.SendPacket(PacketId.PKT_C_INVITE_RESPONSE, inviteResponsePacket);
    }

    #endregion
}
