using Google.Protobuf;
using Protocol;
using UnityEngine;

public class RelayNetManager : BaseNetSession
{
    private static RelayNetManager _instance;
    public static RelayNetManager Instance => _instance ??= new RelayNetManager();
    
    public RelayNetManager()
    {
        this.OnPacketReceivedEvent += HandleRelayPacket;
        this.OnDisconnected += RelayPacketHandler.Instance.OnDisconnected;
    }

    private void HandleRelayPacket(PacketId packetId, byte[] data)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            RelayPacketHandler.Instance.HandleRelayPacket(packetId, data);
        });
    }

    public static void Shutdown()
    {
        if (_instance == null)
            return;

        _instance.ForceShutdown(raiseDisconnected: false);
        _instance = null;
    }
}