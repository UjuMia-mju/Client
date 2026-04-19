using Google.Protobuf;
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

    public void SendGetDbData()
    {
        C_GET_DB_DATA packet = new C_GET_DB_DATA();
        net.SendPacket(PacketId.PKT_C_GET_DB_DATA, packet);
    }

    public void SendGetClearInfo()
    {
        C_GET_CLEAR_INFO packet = new C_GET_CLEAR_INFO();
        net.SendPacket(PacketId.PKT_C_GET_CLEAR_INFO, packet);
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

    public void SendStartStage(int mapId, int chapter, int stageIndex)
    {
        C_START_STAGE packet = new C_START_STAGE
        {
            MapId = mapId,
            Chapter = chapter,
            StageIndex = stageIndex
        };
        
        NetManager.Instance.SendPacket(PacketId.PKT_C_START_STAGE, packet);
    }
    
    #endregion




    //public void SendEnterGame(ulong playerIndex)
    //{
    //    Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");

    //    if (IsHost())
    //    {
    //        // 전체한테 broadcast하는 부분을 추가해야함.
    //        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ENTER, new S_PLAYER_ENTER
    //        {
    //            Player = new PlayerGameInfo
    //            {
    //                PlayerId = (int)GetLocalPlayerId(),
    //                Name = "Host", // packet.Name이 null이면 기본값
    //                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
    //                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
    //            }
    //        });
    //        return;
    //    }
    //    else
    //    {
    //        C_TEST_ENTER_GAME enterGamePacket = new C_TEST_ENTER_GAME
    //        {
    //            PlayerIndex = playerIndex
    //        };
    //        peerNet.SendPacket(PacketId.PKT_C_TEST_ENTER_GAME, enterGamePacket);
    //    }
    //}

    //public void SendChat(string message)
    //{
    //    if (IsHost())
    //    {
    //        return;
    //    }
    //    else
    //    {
    //        C_CHAT chatPacket = new C_CHAT
    //        {
    //            Msg = message
    //        };

    //        peerNet.SendPacket(PacketId.PKT_C_CHAT, chatPacket);
    //    }
        
    //}

    //public void SendMove(Vector3 position, Quaternion rotation, bool force = false)
    //{
    //    // 값이 바뀌지 않았으면 전송하지 않음
    //    if (!force && position == _lastSentPos && rotation == _lastSentRot)
    //    {
    //        return;
    //    }


    //    _lastSentPos = position;
    //    _lastSentRot = rotation;

    //    _movePosInfo.X = position.x;
    //    _movePosInfo.Y = position.y;
    //    _movePosInfo.Z = position.z;

    //    _moveRotInfo.X = rotation.x;
    //    _moveRotInfo.Y = rotation.y;
    //    _moveRotInfo.Z = rotation.z;
    //    _moveRotInfo.W = rotation.w;

    //    if (IsHost())
    //    {
    //        _relayMove.PlayerId = GetLocalPlayerId();
    //        _relayMove.Pos = _movePosInfo;
    //        _relayMove.Rot = _moveRotInfo;
    //        hostNet.BroadcastToPeers(0, PacketId.PKT_S_MOVE, _relayMove);
    //    }
    //    else
    //    {
    //        _movePacket.Pos = _movePosInfo;
    //        _movePacket.Rot = _moveRotInfo;
    //        peerNet.SendPacket(PacketId.PKT_C_MOVE, _movePacket);
    //    }
    //}

    //public void SendAnimation(AnimState animState)
    //{
    //    if (IsHost())
    //    {
    //        _relayAnim.PlayerId = GetLocalPlayerId();
    //        _relayAnim.State = (int)animState;
    //        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ANIMATION, _relayAnim);
    //    }
    //    else
    //    {
    //        _animPacket.State = (int)animState;
    //        peerNet.SendPacket(PacketId.PKT_C_PLAYER_ANIMATION, _animPacket);
    //    }
    //}

    //// 플레이어의 아이템 부착을 송신합니다.
    //// NOTE : ObjectId의 ToolType은 현재 사용하지 않습니다.
    //// 곡괭이 등 도구도 ItemManager에서 고유 itemId를 부여받으므로 ItemId로 통일합니다.
    //// Type 필드는 아이템 / 도구 종류 분기용으로만 사용합니다.
    //// 이하 움직임 패킷도 동일합니다. 의견공유 바랍니다.
    //public void SendItemAttached(Items itemData)
    //{
    //    // 전달받은 아이템의 태그가 "Item"인지 확인
    //    if (itemData.gameObject.CompareTag(Define.Tag.ITEM))
    //    {
    //        if (IsHost())
    //        {
    //            S_OBJECT_PICKUP packet = new S_OBJECT_PICKUP
    //            {
    //                Success = true,
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Item,
    //                    ItemId = (ulong)itemData.itemId
    //                },
    //                PlayerId = GetLocalPlayerId(),

    //                ErrorMsg = ""
    //            };

    //            hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_PICKUP, packet);
    //            return;
    //        }

    //        else
    //        {
    //            C_OBJECT_PICKUP packet = new C_OBJECT_PICKUP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Item,
    //                    ItemId = (ulong)itemData.itemId
    //                }
    //            };
    //            peerNet.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
    //        }
    //    }

    //    // 그것이 아니면 곡괭이 등의 도구임.

    //    // 1. 곡괭이
    //    else if (itemData.gameObject.CompareTag(Define.Tag.PICKAXE))
    //    {
    //        if (IsHost())
    //        {
    //            S_OBJECT_PICKUP packet = new S_OBJECT_PICKUP
    //            {
    //                Success = true,
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Tool,
    //                    ItemId = (ulong)itemData.itemId
    //                },
    //                PlayerId = GetLocalPlayerId(),

    //                ErrorMsg = ""
    //            };

    //            hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_PICKUP, packet);
    //            return;
    //        }

    //        else
    //        {
    //            C_OBJECT_PICKUP packet = new C_OBJECT_PICKUP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Tool,
    //                    ItemId = (ulong)itemData.itemId
    //                }
    //            };
    //            peerNet.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
    //        }
    //    }
    //}

    //public void SendItemDetatched(Items itemData)
    //{
    //    // 전달받은 아이템의 태그가 "Item"인지 확인
    //    if (itemData.gameObject.CompareTag(Define.Tag.ITEM))
    //    {
    //        if (IsHost())
    //        {
    //            S_OBJECT_DROP packet = new S_OBJECT_DROP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Item,
    //                    ItemId = (ulong)itemData.itemId
    //                },
    //                PlayerId = GetLocalPlayerId()
    //            };

    //            hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_DROP, packet);
    //            return;
    //        }

    //        else
    //        {
    //            C_OBJECT_DROP packet = new C_OBJECT_DROP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Item,
    //                    ItemId = (ulong)itemData.itemId
    //                }
    //            };
    //            peerNet.SendPacket(PacketId.PKT_C_OBJECT_DROP, packet);
    //        }
    //    }

    //    // 그것이 아니면 곡괭이 등의 도구임.

    //    // 1. 곡괭이
    //    else if (itemData.gameObject.CompareTag(Define.Tag.PICKAXE))
    //    {
    //        if (IsHost())
    //        {
    //            S_OBJECT_DROP packet = new S_OBJECT_DROP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Tool,
    //                    ItemId = (ulong)itemData.itemId
    //                },
    //                PlayerId = GetLocalPlayerId()
    //            };

    //            hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_DROP, packet);
    //            return;
    //        }

    //        else
    //        {
    //            C_OBJECT_DROP packet = new C_OBJECT_DROP
    //            {
    //                ObjectId = new ObjectId
    //                {
    //                    Type = ObjectType.Tool,
    //                    ItemId = (ulong)itemData.itemId
    //                }
    //            };
    //            peerNet.SendPacket(PacketId.PKT_C_OBJECT_DROP, packet);
    //        }
    //    }
    //}

    //public void SendItemOrToolMove(Items itemData, Vector3 position, Quaternion rotation)
    //{
    //    bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
    //    bool isTool = itemData.gameObject.CompareTag(Define.Tag.PICKAXE);

    //    if (!isItem && !isTool) return;

    //    ObjectType type = isItem ? ObjectType.Item : ObjectType.Tool;

    //    PosInfo posInfo = new PosInfo { X = position.x, Y = position.y, Z = position.z };
    //    RotInfo rotInfo = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w };
    //    ObjectId objectId = new ObjectId { Type = type, ItemId = (ulong)itemData.itemId };

    //    if (IsHost())
    //    {
    //        S_OBJECT_MOVE packet = new S_OBJECT_MOVE { ObjectId = objectId, Pos = posInfo, Rot = rotInfo };
    //        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_MOVE, packet);
    //    }
    //    else
    //    {
    //        C_OBJECT_MOVE packet = new C_OBJECT_MOVE { ObjectId = objectId, Pos = posInfo, Rot = rotInfo };
    //        peerNet.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
    //    }

    //}


}
