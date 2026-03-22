using UnityEngine;
using Protocol;

public class PacketDispatcher : Singleton<PacketDispatcher>
{
    private bool IsHost()
    {
        return ConnectManager.Instance != null && ConnectManager.Instance.isHost;
    }

    private ulong GetLocalPlayerId()
    {
        // 로그인 후 세팅되는 값 사용
        return (ulong)NetManager.Instance._playerId;
    }

    // ==================== 높은 수준의 전송 메서드들 ====================
    NetManager net = NetManager.Instance;
    PeerNetManager peerNet = PeerNetManager.Instance;
    public void SendLogin(string userId, string password)
    {
        C_LOGIN loginPacket = new C_LOGIN
        {
            UserId = userId,
            Psw = password
        };

        net.SendPacket(PacketId.PKT_C_LOGIN, loginPacket);
    }

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
        C_CHAT chatPacket = new C_CHAT
        {
            Msg = message
        };

        net.SendPacket(PacketId.PKT_C_CHAT, chatPacket);
    }

    public void SendMove(Vector3 position, Quaternion rotation)
    {
        PosInfo posInfo = new PosInfo
        {
            X = position.x,
            Y = position.y,
            Z = position.z
        };

        // 이 부분에서 쿼터니언이 오일러각으로 변환되고 있었습니다. 해당 부분을 수정했습니다.
        RotInfo rotInfo = new RotInfo
        {
            X = rotation.x,
            Y = rotation.y,
            Z = rotation.z,
            W = rotation.w
        };

        if (IsHost())
        {
            S_MOVE relay = new S_MOVE
            {
                PlayerId = GetLocalPlayerId(),
                Pos = posInfo,
                Rot = rotInfo
            };

            // senderPeerId는 호스트 자체를 의미하는 예약값(예: 0)
            HostNetManager.Instance.BroadcastToPeers(0, PacketId.PKT_S_MOVE, relay);
            return;
        }
        else
        {
            C_MOVE movePacket = new C_MOVE
            {
                Pos = posInfo,
                Rot = rotInfo
            };

            peerNet.SendPacket(PacketId.PKT_C_MOVE, movePacket);
        }
    }

    public void SendAnimation(AnimState animState)
    {
        if (IsHost())
        {
            S_PLAYER_ANIMATION relay = new S_PLAYER_ANIMATION
            {
                PlayerId = GetLocalPlayerId(),
                State = (int)animState
            };

            HostNetManager.Instance.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ANIMATION, relay);
            return;
        }
        else
        {
            C_PLAYER_ANIMATION animationPacket = new C_PLAYER_ANIMATION
            {
                State = (int)animState
            };
            peerNet.SendPacket(PacketId.PKT_C_PLAYER_ANIMATION, animationPacket);
        }
    }

    // 패킷 보내는거 배치파일 실행해서 코드 자동생성 해야 함. 지금 안 됨.
    //public void SendPlayerStat(int hpData, float oxygenData)
    //{
    //    C_PLAYER_STAT_EVENT statPacket = new C_PLAYER_STAT_EVENT
    //    {
    //        Hp = hpData,
    //        Oxygen = oxygenData
    //    };
    //    SendPacket(PacketId.PKT_C_PLAYER_STAT_EVENT, statPacket);
    //}

    public void SendItemAttached(Items itemData)
    {
        C_OBJECT_PICKUP packet = new C_OBJECT_PICKUP
        {
            ObjectId = new ObjectId
            {
                Type = ObjectType.Item,
                ItemId = (ulong)itemData.itemId
            }
        };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
    }

    public void SendItemDetatched(Items itemData)
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

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
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

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
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

        net.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
    }


    //public void SendCraftingList(List<string> data)
    //{
    //    C_WORKBENCH_LIST craftingListPacket = new C_WORKBENCH_LIST();
    //    craftingListPacket.ItemNames.AddRange(data);
    //    SendPacket(PacketId.PKT_C_WORKBENCH, craftingListPacket);
    //}

    /// <summary>
    /// 핵심: 프로토콜 메시지를 패킷으로 변환하고 Send 호출
    /// </summary>
}
