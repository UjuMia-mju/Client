using UnityEngine;
using Google.Protobuf;
using Protocol;

public class PacketHandler : Singleton<PacketHandler>
{
    // ==================== 높은 수준의 전송 메서드들 ====================
    NetManager net = NetManager.Instance;
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
        C_ENTER_GAME enterGamePacket = new C_ENTER_GAME
        {
            PlayerIndex = playerIndex
        };

        //SendPacket(PacketId.PKT_C_ENTER_GAME, enterGamePacket);
        net.SendPacket(PacketId.PKT_C_TEST_ENTER_GAME, enterGamePacket);
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

        C_MOVE movePacket = new C_MOVE
        {
            Pos = posInfo,
            Rot = rotInfo
        };

        net.SendPacket(PacketId.PKT_C_MOVE, movePacket);
    }

    public void SendAnimation(AnimState animState)
    {
        C_PLAYER_ANIMATION animationPacket = new C_PLAYER_ANIMATION
        {
            State = (int)animState
        };
        net.SendPacket(PacketId.PKT_C_PLAYER_ANIMATION, animationPacket);
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
        net.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
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
        net.SendPacket(PacketId.PKT_C_OBJECT_DROP, packet);
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

        net.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
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
