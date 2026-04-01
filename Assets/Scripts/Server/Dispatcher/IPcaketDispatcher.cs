using UnityEngine;
using Protocol;

public interface IPcaketDispatcher
{
    void SendEnterGame(ulong playerIndex);
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
}
