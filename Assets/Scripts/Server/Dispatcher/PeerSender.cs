using UnityEngine;
using Protocol;

public class PeerSender : IPcaketDispatcher
{
    PeerNetManager peerNet = PeerNetManager.Instance;
    private readonly PosInfo _movePosInfo = new PosInfo();
    private readonly RotInfo _moveRotInfo = new RotInfo();
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;


    // 재사용 가능한 패킷 객체들 (값이 자주 바뀌는 패킷들은 매번 새로 생성하지 않고 재사용)
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
        C_CHAT chatPacket = new C_CHAT
        {
            Msg = message
        };

        peerNet.SendPacket(PacketId.PKT_C_CHAT, chatPacket);
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
        //
    }
    public void SendItemDetatched(Items itemData)
    {
        //
    }
    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        //
    }
    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
    {
        //
    }
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
        Debug.Log("저도 보냅니다!!!!" + targetPlayerId + ", eventType=" + eventType); // 여기가 0이 나오고 있음,
        peerNet.SendPacket(PacketId.PKT_C_PLAYER_STAT_EVENT, _eventPacket);
    }

    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        // Peer는 Broadcast할 권한이 없으므로 이 메서드는 빈 구현으로 남겨둡니다.
        Debug.LogWarning("PeerSender.BroadcastStatResult called, but peers should not broadcast stat results.");
    }
}
