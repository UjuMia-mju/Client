using Protocol;

public class NetManager : BaseNetSession
{
    private static NetManager _instance;
    public static NetManager Instance => _instance ??= new NetManager();
    public int _playerId;
    public string PlayerName { get; private set; }
    public int PlayerTag { get; private set; }
    public NetManager()
    {
        // 이벤트 구독: 패킷 도착 시 HandlePacket 호출
        this.OnPacketReceivedEvent += HandlePacket;
        this.OnDisconnected += PacketHandler.Instance.OnDisconnected;
    }

    private void HandlePacket(PacketId packetId, byte[] data)
    {
        // Unity 메인스레드에서 처리 필요시
        MainThreadDispatcher.Enqueue(() =>
        {
            PacketHandler.Instance.HandlePacket(packetId, data);
        });
    }
}

