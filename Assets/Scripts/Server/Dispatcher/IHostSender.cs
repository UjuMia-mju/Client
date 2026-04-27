using UnityEngine;
using Protocol;
using Google.Protobuf;
public interface IHostSender
{
    void BroadcastToPeers(PacketId packetId, IMessage packet);
    void BroadcastEnterGame(ulong playerIndex);
    void BroadcastChat(string message);
    void SendMove(Vector3 position, Quaternion rotation);
    void SendAnimation(AnimState animState);
    void BroadcastStatResult(ulong targetId, int hp, float oxygen);
    void BroadcastItemMove(int itemId, Vector3 position, Quaternion rotation);
    void BroadcastItemAttached(Items itemData);
    void BroadcastItemDetached(Items itemData);
    void BroadcastFurnanceSmeltStart(int furnaceId, int objectId, int meltTime);
    void BroadcastFurnanceSmeltComplete(int objectId, int furnaceId, ItemType resultItem);
    void BroadcastFurnaceRetrieve(int furnaceId, ItemType retrievedItem);

    void BroadcastObjectSpawn(Items item, Vector3 position, Quaternion rotation);
    void BroadcastObjectDestroy(int itemId);

    void BroadcastSpaceshipUpdate(string itemStringKey, int currentCount);
    void BroadcastSpaceshipComplete(bool success);

    void BroadcastTimerSync(float remainingTime);

    // 자원: 호스트 → 피어
    void BroadcastResourceSpawn(ResourceObject resource);
    void BroadcastResourceDestroy(int resourceId);
}
