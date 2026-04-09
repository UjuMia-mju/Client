using UnityEngine;
using Protocol;

public class PeerSender : IClientSender
{
    PeerNetManager peerNet = PeerNetManager.Instance;
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    private readonly C_MOVE _movePacket = new C_MOVE();
    private readonly C_PLAYER_ANIMATION _animPacket = new C_PLAYER_ANIMATION();
    private readonly C_PLAYER_STAT_EVENT _eventPacket = new C_PLAYER_STAT_EVENT();

    public void SendEnterGame(ulong playerIndex)
    {
        Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");
        C_TEST_ENTER_GAME enterGamePacket = new C_TEST_ENTER_GAME
        {
            PlayerIndex = playerIndex
        };
        peerNet.SendPacket(PacketId.PKT_C_TEST_ENTER_GAME, enterGamePacket);
    }

    public void SendChat(string message)
    {
        C_CHAT chatPacket = new C_CHAT { Msg = message };
        peerNet.SendPacket(PacketId.PKT_C_CHAT, chatPacket);
    }

    public void SendMove(Vector3 position, Quaternion rotation)
    {
        if (position == _lastSentPos && rotation == _lastSentRot) return;

        _lastSentPos = position;
        _lastSentRot = rotation;

        _movePosInfo.X = position.x;
        _movePosInfo.Y = position.y;
        _movePosInfo.Z = position.z;

        _moveRotInfo.X = rotation.x;
        _moveRotInfo.Y = rotation.y;
        _moveRotInfo.Z = rotation.z;
        _moveRotInfo.W = rotation.w;

        _movePacket.Pos = _movePosInfo;
        _movePacket.Rot = _moveRotInfo;
        peerNet.SendPacket(PacketId.PKT_C_MOVE, _movePacket);
    }

    public void SendAnimation(AnimState animState)
    {
        _animPacket.State = (int)animState;
        peerNet.SendPacket(PacketId.PKT_C_PLAYER_ANIMATION, _animPacket);
    }

    public void SendItemAttached(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.PICKAXE);
        if (!isItem && !isTool) return;

        C_OBJECT_PICKUP packet = new C_OBJECT_PICKUP
        {
            ObjectId = new ObjectId
            {
                Type = isItem ? ObjectType.Item : ObjectType.Tool,
                ItemId = (ulong)itemData.itemId
            }
        };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_PICKUP, packet);
    }

    public void SendItemDetatched(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.PICKAXE);
        if (!isItem && !isTool) return;

        C_OBJECT_DROP packet = new C_OBJECT_DROP
        {
            ObjectId = new ObjectId
            {
                Type = isItem ? ObjectType.Item : ObjectType.Tool,
                ItemId = (ulong)itemData.itemId
            }
        };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_DROP, packet);
    }

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        C_OBJECT_MOVE packet = new C_OBJECT_MOVE
        {
            ObjectId = new ObjectId { Type = ObjectType.Item, ItemId = (ulong)itemId },
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z },
            Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
        };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_MOVE, packet);
    }

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation) { }

    public void SendPlayerStatEvent(
        StatEventType eventType,
        ulong targetPlayerId,
        DamageEventData damage = null,
        HealEventData heal = null,
        OxygenEventData oxygen = null,
        ItemUseEventData itemUse = null
    )
    {
        _eventPacket.TargetPlayerId = targetPlayerId;
        _eventPacket.EventType = eventType;

        switch (eventType)
        {
            case StatEventType.DamageTaken:
                if (damage != null) _eventPacket.Damage = damage;
                break;
            case StatEventType.Healed:
                if (heal != null) _eventPacket.Heal = heal;
                break;
            case StatEventType.OxygenChanged:
                if (oxygen != null) _eventPacket.Oxygen = oxygen;
                break;
            case StatEventType.ItemUsed:
                if (itemUse != null) _eventPacket.ItemUse = itemUse;
                break;
        }
        peerNet.SendPacket(PacketId.PKT_C_PLAYER_STAT_EVENT, _eventPacket);
    }

    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        Debug.LogWarning("PeerSender.BroadcastStatResult: 피어는 브로드캐스트 권한이 없습니다.");
    }

    //용광로 요청
    public void SendFurnanceSmeltRequest(ulong objectId, int furnaceId)
    {
        ObjectId objectId_p = new ObjectId { ItemId = objectId, Type = ObjectType.Item };

        C_OBJECT_SMELT reqPacket = new C_OBJECT_SMELT { ObjectId = objectId_p, FurnaceId = furnaceId };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_SMELT, reqPacket);
    }

    // 용광로에서 아이템 회수 요청
    public void SendFurnaceRetrieveRequest(int furnaceId)
    {
        C_FURNACE_RETRIEVE reqPacket = new C_FURNACE_RETRIEVE { FurnaceId = furnaceId };
        peerNet.SendPacket(PacketId.PKT_C_FURNACE_RETRIEVE, reqPacket);
    }

    public void SendObjectSpawn(string itemStringKey, Vector3 position, Quaternion rotation)
    {
        C_OBJECT_SPAWN packet = new C_OBJECT_SPAWN
        {
            ItemStringKey = itemStringKey,
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z },
            Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
        };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_SPAWN, packet);
    }

    public void SendObjectDestroy(int itemId)
    {
        C_OBJECT_DESTROY packet = new C_OBJECT_DESTROY { ItemId = itemId };
        peerNet.SendPacket(PacketId.PKT_C_OBJECT_DESTROY, packet);
    }


}
