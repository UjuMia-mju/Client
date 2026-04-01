using UnityEngine;
using Protocol;
public class PacketSender : MonoBehaviorSingleton<PacketSender>
{
    private IPcaketDispatcher _sender;

    public void Init(bool isHost)
    {
        if (isHost)
        {
            _sender = new HostSender();
        }
        else
        {
            _sender = new PeerSender();
        }
    }

    public void SendEnterGame(ulong playerIndex)
    {
        _sender.SendEnterGame(playerIndex);
    }

    public void SendChat(string message)
    {
        _sender.SendChat(message);
    }

    public void SendMove(Vector3 position, Quaternion rotation)
    {
        _sender.SendMove(position, rotation);
    }

    public void SendAnimation(AnimState animState)
    {
        _sender.SendAnimation(animState);
    }

    public void SendItemAttached(Items itemData)
    {
        _sender.SendItemAttached(itemData);
    }

    public void SendItemDetatched(Items itemData)
    {
        _sender.SendItemDetatched(itemData);
    }

    public void SendItemMove(int itemId, Vector3 position, Quaternion rotation)
    {
        _sender.SendItemMove(itemId, position, rotation);
    }

    public void SendToolMove(ToolType data, Vector3 position, Quaternion rotation)
    {
        _sender.SendToolMove(data, position, rotation);
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
        _sender.SendPlayerStatEvent(eventType, targetPlayerId, damage, heal, oxygen, itemUse);
    }
}
