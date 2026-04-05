using UnityEngine;
using Protocol;

public class HostSender : IHostSender
{
    HostNetManager hostNet = HostNetManager.Instance;
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;


    // 재사용 가능한 패킷 객체들 (값이 자주 바뀌는 패킷들은 매번 새로 생성하지 않고 재사용)
    private readonly S_MOVE _movePacket = new ();
    private readonly S_PLAYER_ANIMATION _animPacket = new ();
    private readonly S_PLAYER_STAT _eventPacket = new ();

    public ulong GetLocalPlayerId()
    {
        // 로그인 후 세팅되는 값 사용
        return (ulong)NetManager.Instance._playerId;
    }

    public void BroadcastEnterGame(ulong playerIndex)
    {
        Debug.Log($"Sending EnterGame for playerIndex: {playerIndex}");

        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_ENTER, new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = (int)GetLocalPlayerId()
            }
        });
        return;
    }
    public void BroadcastChat(string message)
    {
        
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
        //Debug.Log($"Broadcasting stat result for Player {targetPlayerId}: HP={hp}, Oxygen={oxygen}");

        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_STAT, _eventPacket);
    }

    public void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime)
    {
        S_OBJECT_SMELT startPacket = new S_OBJECT_SMELT { 
            ObjectId = new ObjectId { ItemId = (ulong)objectId, Type = ObjectType.Item}, 
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

    // 용광로에서 아이템 회수 알림
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
}
