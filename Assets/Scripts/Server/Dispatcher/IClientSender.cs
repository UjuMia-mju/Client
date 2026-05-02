using UnityEngine;
using Protocol;

public interface IClientSender
{
    void SendEnterGame();
    void SendChat(string message);
    void SendMove(Vector3 position, Quaternion rotation);
    void SendAnimation(AnimState animState);
    void SendItemAttached(Items itemData);
    void SendItemDetatched(Items itemData);
    void SendItemMove(int itemId, Vector3 position, Quaternion rotation);
    void SendToolMove(ToolType data, Vector3 position, Quaternion rotation);
    void SendPlayerStatEvent(
        StatEventType eventType,
        ulong targetPlayerId,
        DamageEventData damage = null,
        HealEventData heal = null,
        OxygenEventData oxygen = null,
        ItemUseEventData itemUse = null
    );
    void BroadcastStatResult(ulong targetId, int hp, float oxygen);
    void SendFurnanceSmeltRequest(ulong objectId, int furnaceId);
    void SendFurnaceRetrieveRequest(int furnaceId);
    void SendObjectSpawn(string itemStringKey, Vector3 position, Quaternion rotation);
    void SendObjectDestroy(int itemId);

    void SendSpaceshipInsert(string itemStringKey, int itemId);

    // 자원: 피어 → 호스트, "이 자원을 1회 타격했다"
    void SendResourceHit(int resourceId);

    // 플레이어 사망 보고\
    void SendPlayerDead(ulong playerId);
}
