using UnityEngine;
using Protocol;
using System;
public class PacketSender : MonoBehaviorSingleton<PacketSender>
{
    private IClientSender clientSender;
    private IHostSender hostSender;

    public void Init(bool isHost)
    {
        if (isHost)
        {
            var sender = new HostSender();
            clientSender = null; // 호스트는 클라이언트 기능이 없음
            hostSender = sender;
        }
        else
        {
            clientSender = new PeerSender();
            hostSender = null; // 피어는 서버 기능이 없음
        }
    }

    private void TryClientSend(Action sendReq)
    {
        if (clientSender == null)
            Debug.LogWarning("[PacketSender] 클라이언트 송신 권한이 없습니다.");
        else
            sendReq?.Invoke();
    }

    private void TryHostBroadcast(Action broadcastReq)
    {
        if (hostSender == null)
            Debug.LogWarning("[PacketSender] 일반 클라이언트(Peer)는 브로드캐스트 권한이 없습니다.");
        else
            broadcastReq?.Invoke();
    }

    #region Client Requests 
    // (클라이언트 -> 호스트 요청)
    public void SendEnterGame(ulong playerIndex)
        => TryClientSend(() => clientSender.SendEnterGame(playerIndex));

    public void SendChat(string message)
        => TryClientSend(() => clientSender.SendChat(message));

    public void SendMove(Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendMove(position, rotation));

    public void SendAnimation(AnimState animState)
        => TryClientSend(() => clientSender.SendAnimation(animState));

    public void SendItemAttached(Items itemData)
        => TryClientSend(() => clientSender.SendItemAttached(itemData));

    public void SendItemDetatched(Items itemData)
        => TryClientSend(() => clientSender.SendItemDetatched(itemData));

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendItemMove(itemId, position, rotation));

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendToolMove(data, position, rotation));

    public void SendPlayerStatEvent(StatEventType eventType, ulong targetPlayerId, DamageEventData damage = null, HealEventData heal = null, OxygenEventData oxygen = null, ItemUseEventData itemUse = null)
        => TryClientSend(() => clientSender.SendPlayerStatEvent(eventType, targetPlayerId, damage, heal, oxygen, itemUse));

    // 용광로 작업 요청 전송
    public void SendFurnanceSmeltRequest(ulong objectId, int furnaceId)
        => TryClientSend(() => clientSender.SendFurnanceSmeltRequest(objectId, furnaceId));
    
    // 용광로에서 아이템 회수 요청 전송
    public void SendFurnaceRetrieveRequest(int furnaceId)
        => TryClientSend(() => clientSender.SendFurnaceRetrieveRequest(furnaceId));


    #endregion


    #region Host Broadcasts 
    //(호스트 -> 전체 클라이언트 전파)

    public void BroadcastPlayerEnter(ulong playerIndex)
        => TryHostBroadcast(() => hostSender.BroadcastEnterGame(playerIndex));

    public void BroadcastChat(string message)
        => TryHostBroadcast(() => hostSender.BroadcastChat(message));

    public void BroadcastMove(Vector3 position, Quaternion rotation)
        => TryHostBroadcast(() => hostSender.SendMove(position, rotation));

    public void BroadcastAnimation(AnimState animState)
        => TryHostBroadcast(() => hostSender.SendAnimation(animState));

    public void BroadcastStatResult(ulong targetId, int hp, float oxygen)
        => TryHostBroadcast(() => hostSender.BroadcastStatResult(targetId, hp, oxygen));

    // 용광로 작업 시작 결과 전파
    public void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime)
        => TryHostBroadcast(() => hostSender.BroadcastFurnanceSmeltStart(furnaceId, objectId, meltTime));

    // 용광로 작업 완료 결과 전파
    public void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem)
        => TryHostBroadcast(() => hostSender.BroadcastFurnanceSmeltComplete(objectId, furnaceId, resultItem));

    // 용광로에서 아이템 회수 결과 전파
    public void BroadcastFurnaceRetrieve(int furnaceId, ItemType retrievedItem)
        => TryHostBroadcast(() => hostSender.BroadcastFurnaceRetrieve(furnaceId, retrievedItem));

    #endregion
}
