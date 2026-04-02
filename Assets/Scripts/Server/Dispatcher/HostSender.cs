using UnityEngine;
using Protocol;

public class HostSender : IPcaketDispatcher
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

    public void SendEnterGame(ulong playerIndex)
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
    public void SendChat(string message)
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
        //권한 없음
        Debug.LogWarning("HostSender.SendPlayerStatEvent called, but stat events should be sent by the HostPlayerStat component.");
    }
    public void BroadcastStatResult(ulong targetPlayerId, int hp, float oxygen)
    {
        _eventPacket.PlayerId = targetPlayerId;
        Debug.Log($"저 보냅니다!!!! ={targetPlayerId}, hp={hp}, oxygen={oxygen}");
        _eventPacket.Hp = hp;
        _eventPacket.Oxygen = oxygen;

        hostNet.BroadcastToPeers(0, PacketId.PKT_S_PLAYER_STAT, _eventPacket);
    }
}
