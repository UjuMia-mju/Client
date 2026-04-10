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
            hostSender = new HostSender();
            clientSender = null;
        }
        else
        {
            clientSender = new PeerSender();
            hostSender = null;
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
    public void SendEnterGame(ulong playerIndex)
        => TryClientSend(() => clientSender.SendEnterGame(playerIndex));

    public void SendChat(string message)
        => TryClientSend(() => clientSender.SendChat(message));

    public void SendMove(Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendMove(position, rotation));

    public void SendAnimation(AnimState animState)
        => TryClientSend(() => clientSender.SendAnimation(animState));

    // 아이템 집기: 호스트/피어 분기
    public void SendItemAttached(Items itemData)
    {
        if (hostSender != null)
            hostSender.BroadcastItemAttached(itemData);
        else
            TryClientSend(() => clientSender.SendItemAttached(itemData));
    }

    // 아이템 놓기: 호스트/피어 분기
    public void SendItemDetatched(Items itemData)
    {
        if (hostSender != null)
            hostSender.BroadcastItemDetached(itemData);
        else
            TryClientSend(() => clientSender.SendItemDetatched(itemData));
    }

    // 아이템 이동: 호스트/피어 분기
    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        if (hostSender != null)
            hostSender.BroadcastItemMove(itemId, position, rotation);
        else
            TryClientSend(() => clientSender.SendItemMove(itemId, position, rotation));
    }

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendToolMove(data, position, rotation));

    public void SendPlayerStatEvent(StatEventType eventType, ulong targetPlayerId, DamageEventData damage = null, HealEventData heal = null, OxygenEventData oxygen = null, ItemUseEventData itemUse = null)
        => TryClientSend(() => clientSender.SendPlayerStatEvent(eventType, targetPlayerId, damage, heal, oxygen, itemUse));

    public void SendFurnanceSmeltRequest(ulong objectId, int furnaceId)
        => TryClientSend(() => clientSender.SendFurnanceSmeltRequest(objectId, furnaceId));

    public void SendFurnaceRetrieveRequest(int furnaceId)
        => TryClientSend(() => clientSender.SendFurnaceRetrieveRequest(furnaceId));

    public void SendObjectSpawn(Items item, Vector3 position, Quaternion rotation)
    {
        if (hostSender != null)
            TryHostBroadcast(() => hostSender.BroadcastObjectSpawn(item, position, rotation));
        else
            TryClientSend(() => clientSender.SendObjectSpawn(item.itemStringKey, position, rotation));
    }

    public void SendObjectDestroy(int itemId)
    {
        if (hostSender != null)
            TryHostBroadcast(() => hostSender.BroadcastObjectDestroy(itemId));
        else
            TryClientSend(() => clientSender.SendObjectDestroy(itemId));
    }

    public void SendSpaceshipInsert(string itemStringKey, int itemId)
        => TryClientSend (() => clientSender.SendSpaceshipInsert(itemStringKey, itemId));

    #endregion

    #region Host Broadcasts
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

    public void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime)
        => TryHostBroadcast(() => hostSender.BroadcastFurnanceSmeltStart(furnaceId, objectId, meltTime));

    public void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem)
        => TryHostBroadcast(() => hostSender.BroadcastFurnanceSmeltComplete(objectId, furnaceId, resultItem));

    public void BroadcastFurnaceRetrieve(int furnaceId, ItemType retrievedItem)
        => TryHostBroadcast(() => hostSender.BroadcastFurnaceRetrieve(furnaceId, retrievedItem));

    public void BroadcastObjectSpawn(Items item, Vector3 position, Quaternion rotation)
        => TryHostBroadcast(() => hostSender.BroadcastObjectSpawn(item, position, rotation));

    public void BroadcastObjectDestroy(int itemId)
        => TryHostBroadcast(() => hostSender.BroadcastObjectDestroy(itemId));

    public void BroadcastSpaceshipUpdate(int currentIndex)
        => TryHostBroadcast(() => hostSender.BroadcastSpaceshipUpdate(currentIndex));

    public void BroadcastSpaceshipComplete(bool success)
        => TryHostBroadcast(() => hostSender.BroadcastSpaceshipComplete(success));
    #endregion
}
