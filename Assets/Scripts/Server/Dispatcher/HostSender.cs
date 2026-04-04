using UnityEngine;
using Protocol;

public class HostSender : IPcaketDispatcher
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

    public void SendEnterGame(ulong playerIndex)
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

    public void SendItemAttached(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.PICKAXE);
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

    public void SendItemDetatched(Items itemData)
    {
        bool isItem = itemData.gameObject.CompareTag(Define.Tag.ITEM);
        bool isTool = itemData.gameObject.CompareTag(Define.Tag.PICKAXE);
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

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        S_OBJECT_MOVE packet = new S_OBJECT_MOVE
        {
            ObjectId = new ObjectId { Type = ObjectType.Item, ItemId = (ulong)itemId },
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z },
            Rot = new RotInfo { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
        };
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_OBJECT_MOVE, packet);
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
        Debug.LogWarning("HostSender.SendPlayerStatEvent: 호스트는 stat 이벤트를 보낼 권한이 없습니다.");
    }

    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        _eventPacket.PlayerId = targetPlayerId;
        _eventPacket.Hp = hp;
        _eventPacket.Oxygen = oxygen;
        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_STAT, _eventPacket);
    }
}
