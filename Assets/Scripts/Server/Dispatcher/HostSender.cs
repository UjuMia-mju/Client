using UnityEngine;
using Protocol;
using Google.Protobuf;

public class HostSender : IHostSender
{
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    private readonly S_MOVE _movePacket = new S_MOVE();
    private readonly S_PLAYER_ANIMATION _animPacket = new S_PLAYER_ANIMATION();
    private readonly S_PLAYER_STAT _eventPacket = new S_PLAYER_STAT();
    private readonly S_OBJECT_MOVE _objectMovePacket = new S_OBJECT_MOVE();

    public ulong GetLocalPlayerId() => (ulong)NetManager.Instance._playerId;

    private void BroadcastRelayPacket(PacketId packetId, IMessage innerPacket)
    {
        //Debug.Log($"12313123131312313123");
        var payload = innerPacket.ToByteString();
        var relayPacket = new C_RELAY_PACKET
        {
            RequireHostAuthority = false,
            PacketId = (uint)packetId,
            Payload = payload
        };

        //RelayNetManager.Instance.SendPacket(PacketId.PKT_C_RELAY_PACKET, relayPacket);
        NetManager.Instance.SendPacket(PacketId.PKT_C_RELAY_PACKET, relayPacket);
    }

    // packet 자체를 바로 Relay쪽으로 보내는 경우
    public void BroadcastToPeers(PacketId packetId, IMessage packet)
    {
        BroadcastRelayPacket(packetId, packet);
    }

    public void BroadcastEnterGame(ulong playerIndex)
    {
        Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");

        BroadcastRelayPacket(PacketId.PKT_S_PLAYER_ENTER, new S_PLAYER_ENTER
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

        BroadcastRelayPacket(PacketId.PKT_S_MOVE, _movePacket);
    }

    public void SendAnimation(AnimState animState)
    {
        _animPacket.PlayerId = GetLocalPlayerId();
        _animPacket.State = (int)animState;

        BroadcastRelayPacket(PacketId.PKT_S_PLAYER_ANIMATION, _animPacket);
    }

    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        _eventPacket.PlayerId = targetPlayerId;
        _eventPacket.Hp = hp;
        _eventPacket.Oxygen = oxygen;

        BroadcastRelayPacket(PacketId.PKT_S_PLAYER_STAT, _eventPacket);
    }

    public void BroadcastItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        _objectMovePacket.ObjectId = new ObjectId { Type = ObjectType.Item, ItemId = (ulong)itemId };
        _objectMovePacket.Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z };
        _objectMovePacket.Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w };

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_MOVE, _objectMovePacket);
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

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_PICKUP, packet);
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

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_DROP, packet);
    }

    public void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime)
    {
        S_OBJECT_SMELT startPacket = new S_OBJECT_SMELT
        {
            ObjectId = new ObjectId { ItemId = (ulong)objectId, Type = ObjectType.Item },
            MeltTime = meltTime,
            FurnaceId = furnaceId
        };

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_SMELT, startPacket);
    }

    public void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem)
    {
        S_SMELT_COMPLETE completePacket = new S_SMELT_COMPLETE
        {
            ObjectId = new ObjectId { ItemId = (ulong)objectId, Type = ObjectType.Item },
            FurnaceId = furnaceId,
            ItemResult = resultItem
        };

        BroadcastRelayPacket(PacketId.PKT_S_SMELT_COMPLETE, completePacket);
        Debug.Log($"녹이기 완료 알림보냄:: {objectId} in Furnace {furnaceId}: Result Item={resultItem}");
    }

    public void BroadcastFurnaceRetrieve(int furnaceId, ItemType resultItem)
    {
        S_FURNACE_RETRIEVE retrievePacket = new S_FURNACE_RETRIEVE
        {
            FurnaceId = furnaceId,
            ItemResult = resultItem
        };

        BroadcastRelayPacket(PacketId.PKT_S_FURNACE_RETRIEVE, retrievePacket);
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

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_SPAWN, packet);
    }

    public void BroadcastObjectDestroy(int itemId)
    {
        S_OBJECT_DESTROY packet = new S_OBJECT_DESTROY { ItemId = itemId };

        BroadcastRelayPacket(PacketId.PKT_S_OBJECT_DESTORY, packet);
        Debug.Log($"[HostSender] BroadcastObjectDestroy: itemId={itemId}");
    }

    public void BroadcastSpaceshipUpdate(string itemStringKey, int currentCount)
    {
        S_SPACESHIP_UPDATE packet = new S_SPACESHIP_UPDATE
        {
            ItemStringKeyMission = itemStringKey,
            CurrentIndex = currentCount
        };
        //hostNet.BroadcastToPeers(0, PacketId.PKT_S_SPACESHIP_UPDATE, packet);
        BroadcastRelayPacket(PacketId.PKT_S_SPACESHIP_UPDATE, packet);
    }

    public void BroadcastSpaceshipComplete(bool success)
    {
        S_SPACESHIP_COMPLETE packet = new S_SPACESHIP_COMPLETE { Success = success };

        BroadcastRelayPacket(PacketId.PKT_S_SPACESHIP_COMPLETE, packet);
    }

    public void BroadcastTimerSync(float remainingTime)
    {
        S_TIMER_SYNC packet = new S_TIMER_SYNC { RemainingTime = remainingTime };

        BroadcastRelayPacket(PacketId.PKT_S_TIMER_SYNC, packet);
    }

    public void BroadcastResourceSpawn(ResourceObject resource)
    {
        if (resource == null) return;

        S_RESOURCE_SPAWN packet = new S_RESOURCE_SPAWN
        {
            ResourceId = resource.resourceId,
            ResourceStringKey = resource.resourceStringKey,
            Pos = new PosInfo
            {
                X = resource.transform.position.x,
                Y = resource.transform.position.y,
                Z = resource.transform.position.z
            }
        };

        BroadcastRelayPacket(PacketId.PKT_S_RESOURCE_SPAWN, packet);
        Debug.Log($"[HostSender] BroadcastResourceSpawn: id={resource.resourceId}, key={resource.resourceStringKey}");
    }

    public void BroadcastResourceDestroy(int resourceId)
    {
        S_RESOURCE_DESTROY packet = new S_RESOURCE_DESTROY { ResourceId = resourceId };

        BroadcastRelayPacket(PacketId.PKT_S_RESOURCE_DESTROY, packet);
        Debug.Log($"[HostSender] BroadcastResourceDestroy: id={resourceId}");
    }
}
