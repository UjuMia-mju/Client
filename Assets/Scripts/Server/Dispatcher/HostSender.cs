using UnityEngine;
using Protocol;

public class HostSender : IHostSender
{
    HostNetManager hostNet = HostNetManager.Instance;
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    private readonly S_MOVE _movePacket = new S_MOVE();
    private readonly S_PLAYER_ANIMATION _animPacket = new S_PLAYER_ANIMATION();
    private readonly S_PLAYER_STAT _eventPacket = new S_PLAYER_STAT();

    public ulong GetLocalPlayerId() => (ulong)NetManager.Instance._playerId;

    public void BroadcastEnterGame(ulong playerIndex)
    {
        Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ENTER, new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = (int)GetLocalPlayerId(),
                Name = "Host",
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        });
    }

    public void BroadcastChat(string message) { }

    public void SendChat(string message) { }

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

        _movePacket.PlayerId = GetLocalPlayerId();
        _movePacket.Pos = _movePosInfo;
        _movePacket.Rot = _moveRotInfo;
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_MOVE, _movePacket);
    }

    public void SendAnimation(AnimState animState)
    {
        _animPacket.PlayerId = GetLocalPlayerId();
        _animPacket.State = (int)animState;
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ANIMATION, _animPacket);
    }

    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        _eventPacket.PlayerId = targetPlayerId;
        _eventPacket.Hp = hp;
        _eventPacket.Oxygen = oxygen;
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_STAT, _eventPacket);
    }

    public void BroadcastItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        S_OBJECT_MOVE packet = new S_OBJECT_MOVE
        {
            ObjectId = new ObjectId { Type = ObjectType.Item, ItemId = (ulong)itemId },
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z },
            Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_MOVE, packet);
    }

    public void BroadcastItemAttached(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.TOOL);
        if (!isItem && !isTool) return;

        S_OBJECT_PICKUP packet = new S_OBJECT_PICKUP
        {
            Success = true,
            ObjectId = new ObjectId
            {
                Type = isItem ? ObjectType.Item : ObjectType.Tool,
                ItemId = (ulong)itemData.itemId
            },
            PlayerId = GetLocalPlayerId(),
            ErrorMsg = ""
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_PICKUP, packet);
    }

    public void BroadcastItemDetached(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.TOOL);
        if (!isItem && !isTool) return;

        S_OBJECT_DROP packet = new S_OBJECT_DROP
        {
            ObjectId = new ObjectId
            {
                Type = isItem ? ObjectType.Item : ObjectType.Tool,
                ItemId = (ulong)itemData.itemId
            },
            PlayerId = GetLocalPlayerId()
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_DROP, packet);
    }

    public void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime)
    {
        S_OBJECT_SMELT startPacket = new S_OBJECT_SMELT
        {
            ObjectId = new ObjectId { ItemId = (ulong)objectId, Type = ObjectType.Item },
            MeltTime = meltTime,
            FurnaceId = furnaceId
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_SMELT, startPacket);
    }

    public void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem)
    {
        S_SMELT_COMPLETE completePacket = new S_SMELT_COMPLETE
        {
            ObjectId = new ObjectId { ItemId = (ulong)objectId, Type = ObjectType.Item },
            FurnaceId = furnaceId,
            ItemResult = resultItem
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_SMELT_COMPLETE, completePacket);
        Debug.Log($"녹이기 완료 알림보냄:: {objectId} in Furnace {furnaceId}: Result Item={resultItem}");
    }

    public void BroadcastFurnaceRetrieve(int furnaceId, ItemType resultItem)
    {
        S_FURNACE_RETRIEVE retrievePacket = new S_FURNACE_RETRIEVE
        {
            FurnaceId = furnaceId,
            ItemResult = resultItem
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_FURNACE_RETRIEVE, retrievePacket);
        Debug.Log($"용광로에서 아이템 회수 알림 보냄:: Furnace {furnaceId}: Result Item={resultItem}");
    }

    public void BroadcastObjectSpawn(Items item, Vector3 position, Quaternion rotation)
    {
        S_OBJECT_SPAWN packet = new S_OBJECT_SPAWN
        {
            ItemId = item.itemId,
            ItemStringKey = item.itemStringKey,
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z },
            Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_SPAWN, packet);
    }

    public void BroadcastObjectDestroy(int itemId)
    {
        S_OBJECT_DESTROY packet = new S_OBJECT_DESTROY { ItemId = itemId };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_DESTORY, packet);
        Debug.Log($"[HostSender] BroadcastObjectDestroy: itemId={itemId}");
    }

    public void BroadcastSpaceshipUpdate(string itemStringKey, int currentCount)
    {
        S_SPACESHIP_UPDATE packet = new S_SPACESHIP_UPDATE
        {
            ItemStringKeyMission = itemStringKey,
            CurrentIndex = currentCount
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_SPACESHIP_UPDATE, packet);
    }

    public void BroadcastSpaceshipComplete(bool success)
    {
        S_SPACESHIP_COMPLETE packet = new S_SPACESHIP_COMPLETE { Success = success };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_SPACESHIP_COMPLETE, packet);
    }

    public void BroadcastTimerSync(float remainingTime)
    {
        S_TIMER_SYNC packet = new S_TIMER_SYNC { RemainingTime = remainingTime };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_TIMER_SYNC, packet);
    }   
}
