using UnityEngine;
using Protocol;
public interface IHostSender
{
    void BroadcastEnterGame(ulong playerIndex);
    void BroadcastChat(string message);
    void SendMove(Vector3 position, Quaternion rotation);
    void SendAnimation(AnimState animState);
    void BroadcastStatResult(ulong targetId, int hp, float oxygen);
    void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime);
    void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem);
}
