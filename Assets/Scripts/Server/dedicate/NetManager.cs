using Protocol;

public class NetManager : BaseNetSession
{
    private static NetManager _instance;
    public static NetManager Instance => _instance ??= new NetManager();
    public ulong _playerId;
    public string PlayerName { get; private set; } = "";
    public int PlayerTag { get; private set; }

    /// <summary>마지막 <see cref="C_LOGIN"/>에 쓴 계정 id(서버가 <c>S_LOGIN.player</c>를 안 줄 때 표시 이름 보조).</summary>
    public string LastAttemptedLoginUserId { get; private set; } = "";

    public void SetLastLoginCredentials(string userId)
    {
        LastAttemptedLoginUserId = userId ?? "";
    }

    /// <summary>S_LOGIN 등으로 받은 로컬 계정 표시 정보(다른 클라이언트로 릴레이되지 않음).</summary>
    public void SetLocalPlayerProfile(string name, int tag)
    {
        PlayerName = name ?? "";
        PlayerTag = tag;
    }

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

