using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;
using UnityEngine;
using System.Threading;

public class PeerSession
{
    public int PeerId { get; set; }
    public Socket Socket { get; set; }
    public RecvBuffer RecvBuffer { get; set; }
}

public class NetManager : Singleton<NetManager>
{
    private Socket _socket; // 서버와의 연결을 위한 소켓 (데디케이트)
    private bool _isConnected = false;
    private const int BUFFER_SIZE = 65536; // 64KB

    // Recv 버퍼
    private RecvBuffer _recvBuffer = new RecvBuffer(BUFFER_SIZE);
    
    // 송신 큐
    private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    private object _sendLock = new object();
    private bool _isSending = false;

    public bool IsConnected => _isConnected;


    // 임시로 정보 저장 (실제 게임에서는 별도의 로그인 관리 필요)
    public int _playerId;

    // ================ Host Mode (Peer 관리) ================
    private bool _isHostMode = false;
    public bool IsHostMode => _isHostMode;
    // 호스트가 상위 데디케이트 서버에 붙는 소켓은 기존 _socket 사용
    // 하위 피어를 받기 위한 리스너/피어 목록은 별도로 사용합니다.
    private Socket _peerListener;
    private int _nextPeerId = 1;
    private readonly object _peerLock = new object();
    // 연결된 peer session 관리
    private Dictionary<int, PeerSession> _peerSessions = new Dictionary<int, PeerSession>();


    #region Connect
    // NetManager.cs의 기존 StartHost(int port)를 아래 형태로 교체
    // upstreamIp/upstreamPort: 데디케이트 서버
    // listenPort: 호스트가 로컬 피어를 받을 포트
    public void StartHost(int listenPort)
    {
        if (_isHostMode)
        {
            Debug.LogWarning("Already in host mode!");
            return;
        }

        // 호스트 모드에서는 먼저 데디케이트 서버에 연결해야 합니다. (실제 게임에서는 로그인/매칭 후에 이 단계가 올 수 있습니다.)
        // if (!_isConnected)
        // {
        //     Debug.LogWarning("Must connect to upstream server before starting host mode!");
        //     return;
        // }

        // 하위 Peer 수신 리스너 시작
        try
        {
            _peerListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _peerListener.Bind(new IPEndPoint(IPAddress.Any, listenPort)); // 모든 인터페이스에서 listenPort로 바인딩 (즉, 다 열어준다는 뜻입니다.)
            _peerListener.Listen(6); // 최대 6명까지 대기열 허용 (혹시 몰라 넉넉하게)
            _peerListener.BeginAccept(OnPeerAcceptCallback, null); // 비동기로 Accept 시작

            Debug.Log($"Host listen started on port {listenPort}");
            _isHostMode = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"StartHost listen failed: {ex.Message}");
        }
    }

    public void Connect(string ip, int port)
    {
        if (_isConnected)
        {
            Debug.LogWarning("Already connected!");
            return;
        }

        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // 비동기 작업 등록
            _socket.BeginConnect(endPoint, OnConnectCallback, null);
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connect failed: {ex.Message}");
        }
    }

    // 연결 성공하면 .NET에서 콜백 호출
    private void OnConnectCallback(IAsyncResult ar)
    {
        try
        {
            _socket.EndConnect(ar);
            _isConnected = true;
            Debug.Log("Connected to server!");

            // Unity 메인 스레드에서 처리
            // MainThreadDispatcher.Enqueue(() =>
            // {
            //     PacketManager.Instance.OnConnected();
            // });

            // 수신 시작
            RegisterRecv();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnConnect failed: {ex.Message}");
        }
    }

    #endregion
    // ------------------- Receive -------------------
    #region Receive
    private void RegisterRecv()
    {
        if (!_isConnected)
            return;

        ArraySegment<byte> segment = _recvBuffer.GetWriteSegment();

        // 수신 등록.
        _socket.BeginReceive(segment.Array, segment.Offset, segment.Count, SocketFlags.None, OnRecvCallback, null);
    }

    private void OnRecvCallback(IAsyncResult ar)
    {
        try
        {
            int bytesRead = _socket.EndReceive(ar);
            Debug.Log($"Received {bytesRead} bytes from server");  // 로그

            if (bytesRead == 0)
            {
                Disconnect("Server closed connection");
                return;
            }

            // 수신 버퍼 업데이트
            if (!_recvBuffer.OnWrite(bytesRead))
            {
                Disconnect("RecvBuffer overflow");
                return;
            }

            // 패킷 처리 (C++ PacketSession::OnRecv와 동일)
            int processedBytes = ProcessPackets();

            if (processedBytes < 0)
            {
                Disconnect("Packet processing failed");
                return;
            }

            if (!_recvBuffer.OnRead(processedBytes))
            {
                Disconnect("RecvBuffer read failed");
                return;
            }

            _recvBuffer.Clean();

            // 다시 수신 대기
            RegisterRecv();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnRecv failed: {ex.Message}");
            Disconnect("Receive error");
        }
    }

    // 패킷 파싱 (C++ PacketSession::OnRecv 로직)
    private int ProcessPackets()
    {
        Debug.Log($"Processing packets in buffer. DataSize: {_recvBuffer.DataSize} bytes");  // 로그
        int processedBytes = 0;

        while (true)
        {
            int dataSize = _recvBuffer.DataSize - processedBytes;

            // 최소 헤더 크기 확인
            if (dataSize < PacketHeader.HeaderSize)
                break;

            // 읽을 수 있는 부분을 가져온다.
            ArraySegment<byte> buffer = _recvBuffer.GetReadSegment();
            // 헤더 파싱
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);

            // 완전한 패킷이 도착했는지 확인
            if (dataSize < header.size)
                break;

            // Unity 메인 스레드에서 패킷 처리
            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize, packetData, 0, packetData.Length);

            MainThreadDispatcher.Enqueue(() =>
            {
                PacketManager.Instance.HandlePacket((PacketId)header.id, packetData);
            });

            processedBytes += header.size;
        }

        return processedBytes;
    }

    // ==================== 높은 수준의 Recv 메서드들 ====================

    #endregion
    // ------------------- Send -------------------
    #region Send
    // 송신 (C++ Session::Send와 동일)
    private void Send(ArraySegment<byte> packet)
    {
        if (!_isConnected)
            return;

        lock (_sendLock)
        {
            _sendQueue.Enqueue(packet);

            if (!_isSending)
            {
                _isSending = true;
                RegisterSend();
            }
        }
    }

    // 송신 등록 (C++ Session::RegisterSend와 동일)
    private void RegisterSend()
    {
        if (!_isConnected)
            return;

        ArraySegment<byte>[] segments;

        lock (_sendLock)
        {
            segments = _sendQueue.ToArray();
            _sendQueue.Clear();
        }

        if (segments.Length == 0)
        {
            _isSending = false;
            return;
        }

        try
        {
            // Scatter-Gather 전송
            _socket.BeginSend(segments, SocketFlags.None, OnSendCallback, segments);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
            Disconnect("Send error");
        }
    }

    private void OnSendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);

            if (bytesSent == 0)
            {
                Disconnect("Send failed");
                return;
            }

            lock (_sendLock)
            {
                if (_sendQueue.Count > 0)
                {
                    RegisterSend();
                }
                else
                {
                    _isSending = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnSend failed: {ex.Message}");
            Disconnect("Send callback error");
        }
    }

    public void SendPacket<T>(PacketId packetId, T packet) where T : IMessage
    {
        try
        {
            byte[] packetData = packet.ToByteArray();
            byte[] sendBuffer = new byte[4 + packetData.Length];

            // 패킷 크기
            Array.Copy(BitConverter.GetBytes((ushort)(4 + packetData.Length)), 0, sendBuffer, 0, 2);

            // 패킷 ID
            Array.Copy(BitConverter.GetBytes((ushort)packetId), 0, sendBuffer, 2, 2);

            // 패킷 데이터
            Array.Copy(packetData, 0, sendBuffer, 4, packetData.Length);

            // 🔑 핵심: ArraySegment로 변환해서 Send 호출
            ArraySegment<byte> packet_segment = new ArraySegment<byte>(sendBuffer);
            Send(packet_segment);

            Debug.Log($"Sent packet: {packetId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SendPacket Error: {ex.Message}");
        }
    }

    #endregion
    // ------------------- Disconnect -------------------
    #region Disconnect Handler
    public void Disconnect(string reason)
    {
        if (!_isConnected)
            return;

        _isConnected = false;
        Debug.Log($"Disconnected: {reason}");

        _socket?.Close();
        _socket = null;

        // MainThreadDispatcher.Enqueue(() =>
        // {
        //     PacketManager.Instance.OnDisconnected();
        // });
    }

    private void OnApplicationQuit()
    {
        Disconnect("Application quit");
    }

    #endregion

    #region Host Peer Management
    private void OnPeerAcceptCallback(IAsyncResult ar)
    {
        Socket peer = null;

        try
        {
            if (_peerListener == null)
            {
                return;
            }

            peer = _peerListener.EndAccept(ar); // Accept 작업 결과 가져오는 메서드

            int peerId = Interlocked.Increment(ref _nextPeerId); // 락보다 성능 좋음. (찾아보니까 하드웨서 레벨에서 원자적으로 처리해준다고 함)

            PeerSession session = new PeerSession
            {
                PeerId = peerId,
                Socket = peer,
                RecvBuffer = new RecvBuffer(BUFFER_SIZE)
            };

            lock (_peerLock)
            {
                _peerSessions[peerId] = session;
            }

            Debug.Log($"Peer connected. peerId={peerId}, endpoint={peer.RemoteEndPoint}");
            // 피어 수신 시작
            RegisterPeerRecv(peerId);  
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnPeerAcceptCallback failed: {ex.Message}");

            // 리스너가 살아있으면 accept 루프 유지
            try
            {
                _peerListener?.BeginAccept(OnPeerAcceptCallback, null);
            }
            catch
            {
                // ignore
            }

            if (peer != null)
            {
                try { peer.Close(); } catch { }
            }
        }
    }

    private void RegisterPeerRecv(int peerId)
    {
        PeerSession session = null;
        lock (_peerLock)
        {
            if (!_peerSessions.TryGetValue(peerId, out session))
                return;
        }

        ArraySegment<byte> segment = session.RecvBuffer.GetWriteSegment();
        // PeerId를 통해 어떤 피어인지 특정.
        session.Socket.BeginReceive(segment.Array, segment.Offset, segment.Count, SocketFlags.None, OnPeerRecvCallback, peerId);
    }

    private void OnPeerRecvCallback(IAsyncResult ar)
    {
        int peerId = (int)ar.AsyncState;

        PeerSession session = null;
        lock (_peerLock)
        {
            if (!_peerSessions.TryGetValue(peerId, out session))
                return;
        }

        try
        {
            int bytesRead = session.Socket.EndReceive(ar);
            if (bytesRead == 0)
            {
                DisconnectPeer(peerId, "Peer closed connection");
                return;
            }

            // 버퍼 업데이트
            if (!session.RecvBuffer.OnWrite(bytesRead))
            {
                DisconnectPeer(peerId, "RecvBuffer overflow");
                return;
            }

            // 패킷 처리
            int processedBytes = ProcessPeerPackets(peerId);

            if (processedBytes < 0 || !session.RecvBuffer.OnRead(processedBytes))
            {
                DisconnectPeer(peerId, "Packet processing failed");
                return;
            }

            session.RecvBuffer.Clean();
            RegisterPeerRecv(peerId);  // 다시 수신 등록
        }
        catch (Exception ex)
        {
            DisconnectPeer(peerId, $"Peer recv error: {ex.Message}");
        }
    }

    private int ProcessPeerPackets(int peerId)
    {
        PeerSession session = null;
        lock (_peerLock)
        {
            if (!_peerSessions.TryGetValue(peerId, out session))
                return 0;
        }

        int processedBytes = 0;

        while (true)
        {
            int dataSize = session.RecvBuffer.DataSize - processedBytes;
            if (dataSize < PacketHeader.HeaderSize)
                break;

            ArraySegment<byte> buffer = session.RecvBuffer.GetReadSegment();
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);

            if (dataSize < header.size)
                break;

            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize,
                packetData, 0, packetData.Length);

            // ★ 핵심: PeerPacketHandler로 전달
            MainThreadDispatcher.Enqueue(() =>
            {
                PeerPacketHandler.Instance.HandlePeerPacket(peerId, (PacketId)header.id, packetData);
            });

            processedBytes += header.size;
        }

        return processedBytes;
    }

    private void DisconnectPeer(int peerId, string reason)
    {
        PeerSession session = null;

        lock (_peerLock)
        {
            if (_peerSessions.TryGetValue(peerId, out session))
            {
                _peerSessions.Remove(peerId);
            }
        }

        if (session == null)
        {
            return;
        }

        try
        {
            if (session.Socket != null && session.Socket.Connected)
            {
                session.Socket.Shutdown(SocketShutdown.Both);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            session.Socket?.Close();
        }
        catch
        {
            // ignore
        }

        Debug.Log($"Peer disconnected. peerId={peerId}, reason={reason}");
    }

    #endregion
}


