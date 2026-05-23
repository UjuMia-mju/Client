using UnityEngine;
using Protocol;
using System;
using Google.Protobuf;

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
    public void SendEnterGame()
        => TryClientSend(() => clientSender.SendEnterGame());

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
    public void SendItemDetatched(Items itemData, bool charged)
    {
        if (hostSender != null)
            hostSender.BroadcastItemDetached(itemData, charged);
        else
            TryClientSend(() => clientSender.SendItemDetatched(itemData, charged));
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

    // 피어 전용: 아이템 스폰 요청 (로컬 스폰 없이 키+위치만 전송)
    public void SendObjectSpawnRequest(string itemStringKey, Vector3 position, Quaternion rotation)
        => TryClientSend(() => clientSender.SendObjectSpawn(itemStringKey, position, rotation));

    // 자원: 피어 → 호스트
    public void SendResourceHit(int resourceId)
        => TryClientSend(() => clientSender.SendResourceHit(resourceId));

    // 플레이어 사망 보고
    public void SendPlayerDead(ulong playerId)
        => TryClientSend(() => clientSender.SendPlayerDead(playerId));
    #endregion

    #region Host Broadcasts
    public void BroadcastToPeers(PacketId packetId, IMessage packet)
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(packetId, packet));

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

    public void BroadcastSpaceshipUpdate(string itemStringKey, int currentCount)
        => TryHostBroadcast(() => hostSender.BroadcastSpaceshipUpdate(itemStringKey, currentCount));

    public void BroadcastSpaceshipComplete(bool success)
        => TryHostBroadcast(() => hostSender.BroadcastSpaceshipComplete(success));

    public void BroadcastTimerSync(float remainingTime)
        => TryHostBroadcast(() => hostSender.BroadcastTimerSync(remainingTime));

    public void BroadcastResourceSpawn(ResourceObject resource)
        => TryHostBroadcast(() => hostSender.BroadcastResourceSpawn(resource));

    public void BroadcastResourceDestroy(int resourceId)
        => TryHostBroadcast(() => hostSender.BroadcastResourceDestroy(resourceId));

    public void BroadcastPlayerDead(ulong playerId)
        => TryHostBroadcast(() => hostSender.BroadCastPlayerDead(playerId));

    public void BroadcastPlayerRevive(ulong playerId, Vector3 pos, Quaternion rot)
        => TryHostBroadcast(() => hostSender.BroadCastPlayerRevive(playerId, pos, rot));

    // [추가] 호스트 → 모든 피어: "스테이지 선택 씬으로 돌아가라" 신호.
    // 일반 릴레이로만 처리되므로 서버 코드 변경 불필요.
    public void BroadcastReturnToStageSelect()
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(
            PacketId.PKT_S_RETURN_TO_STAGE_SELECT,
            new Protocol.S_RETURN_TO_STAGE_SELECT()));

    // 추가 : 호스트 → 모든 피어: "플레이어가 피격당했다" 신호.
    public void BroadcastPlayerHit(ulong victimPlayerId, float freezeSeconds)
        => TryHostBroadcast(() => hostSender.BroadcastPlayerHit(victimPlayerId, freezeSeconds));

    public void BroadcastMonsterSpawn(S_MONSTER_SPAWN packet)
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(PacketId.PKT_S_MONSTER_SPAWN, packet));

    public void BroadcastMonsterDead(S_MONSTER_DEAD packet)
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(PacketId.PKT_S_MONSTER_DEAD, packet));

    // [추가] 몬스터 애니메이션 상태 브로드캐스트 (호스트 전용)
    public void BroadcastMonsterAnimation(S_MONSTER_ANIMATION packet)
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(PacketId.PKT_S_MONSTER_ANIMATION, packet));

    // 몬스터 위치/회전 브로드캐스트 (호스트 전용)
    public void BroadcastMonsterMove(S_MONSTER_MOVE packet)
        => TryHostBroadcast(() => hostSender.BroadcastToPeers(PacketId.PKT_S_MONSTER_MOVE, packet));
    #endregion
}
