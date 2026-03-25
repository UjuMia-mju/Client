using UnityEngine;
using Protocol;


/// <summary>
///  프로토콜 메시지를 패킷으로 변환하여 NetManager 또는 PeerNetManager를 통해 전송하는 클래스
///  한마디로 서버에 보낼건지, 호스트한테 보낼건지 네트워크 경로를 지정해줌.
/// </summary>
public class PacketDispatcher : Singleton<PacketDispatcher>
{

    // 재사용 가능한 패킷 객체들 (값이 자주 바뀌는 패킷들은 매번 새로 생성하지 않고 재사용)
    // 이동 패킷
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private readonly C_MOVE _movePacket = new C_MOVE();
    private readonly S_MOVE _relayMove = new S_MOVE();

    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    // 애니메이션 패킷
    private readonly S_PLAYER_ANIMATION _relayAnim = new S_PLAYER_ANIMATION();
    private readonly C_PLAYER_ANIMATION _animPacket = new C_PLAYER_ANIMATION();

    private bool IsHost()
    {
        return ConnectManager.Instance != null && ConnectManager.Instance.isHost;
    }

    private ulong GetLocalPlayerId()
    {
        // 로그인 후 세팅되는 값 사용
        return (ulong)NetManager.Instance._playerId;
    }
    NetManager net = NetManager.Instance;
    PeerNetManager peerNet = PeerNetManager.Instance;
    HostNetManager hostNet = HostNetManager.Instance;

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


    #region To Host

    public void SendEnterGame(ulong playerIndex)
    {
        Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");
        
        if (IsHost())
        {
            // 전체한테 broadcast하는 부분을 추가해야함.

            return;
        }
        else
        {
            C_TEST_ENTER_GAME enterGamePacket = new C_TEST_ENTER_GAME
            {
                PlayerIndex = playerIndex
            };
            peerNet.SendPacket(PacketId.PKT_C_TEST_ENTER_GAME, enterGamePacket);
        }
    }
   

    public void SendChat(string message)
    {
        if (IsHost())
        {
            return;
        }
        else
        {
            C_CHAT chatPacket = new C_CHAT
            {
                Msg = message
            };

            peerNet.SendPacket(PacketId.PKT_C_CHAT, chatPacket);
        }
        
    }

    public void SendMove(Vector3 position, Quaternion rotation)
    {
        // 값이 바뀌지 않았으면 전송하지 않음
        if (position == _lastSentPos && rotation == _lastSentRot)
        {
            return;
        }


        _lastSentPos = position;
        _lastSentRot = rotation;

        _movePosInfo.X = position.x;
        _movePosInfo.Y = position.y;
        _movePosInfo.Z = position.z;

        _moveRotInfo.X = rotation.x;
        _moveRotInfo.Y = rotation.y;
        _moveRotInfo.Z = rotation.z;
        _moveRotInfo.W = rotation.w;

        if (IsHost())
        {
            _relayMove.PlayerId = GetLocalPlayerId();
            _relayMove.Pos = _movePosInfo;
            _relayMove.Rot = _moveRotInfo;
            hostNet.BroadcastToPeers(0, PacketId.PKT_S_MOVE, _relayMove);
        }
        else
        {
            _movePacket.Pos = _movePosInfo;
            _movePacket.Rot = _moveRotInfo;
            peerNet.SendPacket(PacketId.PKT_C_MOVE, _movePacket);
        }
    }

    public void SendAnimation(AnimState animState)
    {
        if (IsHost())
        {
            _relayAnim.PlayerId = GetLocalPlayerId();
            _relayAnim.State = (int)animState;
            hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ANIMATION, _relayAnim);
        }
        else
        {
            _animPacket.State = (int)animState;
            peerNet.SendPacket(PacketId.PKT_C_PLAYER_ANIMATION, _animPacket);
        }
    }

    public void SendItemAttached(Items itemData)
    {
        if (IsHost())
        {
            return;
        }
        else
        {
            C_OBJECT_PICKUP packet = new C_OBJECT_PICKUP
            {
                ObjectId = new ObjectId
                {
                    Type = ObjectType.Item,
                    ItemId = (ulong)itemData.itemId
                }
            };
            net.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
        }
    }

    public void SendItemDetatched(Items itemData)
    {
        if (IsHost())
        {
            return;
        }
        else
        {
            C_OBJECT_DROP packet = new C_OBJECT_DROP
            {
                ObjectId = new ObjectId
                {
                    Type = ObjectType.Item,
                    ItemId = (ulong)itemData.itemId
                }
            };
            peerNet.SendPacket(PacketId.PKT_C_OBJECT_DROP, packet);
        }
        
    }

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        if (IsHost())
        {
            return;
        }
        else
        {
            PosInfo posInfo = new PosInfo
            {
                X = position.x,
                Y = position.y,
                Z = position.z
            };

            RotInfo rotInfo = new RotInfo
            {
                X = rotation.x,
                Y = rotation.y,
                Z = rotation.z,
                W = rotation.w
            };

            ObjectId objectId = new ObjectId
            {
                Type = ObjectType.Item,
                ItemId = (ulong)itemId
            };

            C_OBJECT_MOVE packet = new C_OBJECT_MOVE
            {
                ObjectId = objectId,
                Pos = posInfo,
                Rot = rotInfo
            };

            peerNet.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
        }
        
    }

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
    {
        if (IsHost())
        {
            return;
        }        
        else
        {
            PosInfo posInfo = new PosInfo
            {
                X = position.x,
                Y = position.y,
                Z = position.z
            };

            RotInfo rotInfo = new RotInfo
            {
                X = rotation.x,
                Y = rotation.y,
                Z = rotation.z,
                W = rotation.w
            };

            ObjectId objectId = new ObjectId
            {
                Type = ObjectType.Tool,
                ToolType = data
            };

            C_OBJECT_MOVE packet = new C_OBJECT_MOVE
            {
                ObjectId = objectId,
                Pos = posInfo,
                Rot = rotInfo
            };

            peerNet.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
        }
        
    }


    #endregion
}
