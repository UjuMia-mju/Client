using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using Protocol;
using UnityEngine;

public class PeerSession
{
    public int PeerId { get; set; }
    public Socket Socket { get; set; }
    public RecvBuffer RecvBuffer { get; set; }
}


// 호스트가 피어로부터 받은 패킷을 처리하는 클래스
// NetManager의 피어 receive 루프에서 호출됨
// 각 C_ 패킷에 대한 이벤트를 정의하여 게임 로직에서 구독할 수 있도록 함
// 예: 플레이어 이동, 채팅 메시지, 애니메이션 상태 변경, 아이템 상호작용 등
public class PeerNetManager : Singleton<PeerNetManager>
{
    // === 공통 ===
    private const int BUFFER_SIZE = 65536;

    // === 호스트 모드 ===
    private bool _isHostMode = false;
    public bool IsHostMode => _isHostMode;
    private const int maxPeers = 4; // 최대 피어 수 (호스트 포함 총 5명)
    private Socket _peerListener;
    private int _nextPeerId = 0;
    private readonly object _peerLock = new object();
    private Dictionary<int, PeerSession> _peerSessions = new Dictionary<int, PeerSession>();

    // === 클라이언트 모드 ===
    private Socket _hostSocket;
    private bool _isClientConnected = false;
    private RecvBuffer _clientRecvBuffer = new RecvBuffer(BUFFER_SIZE);

    private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    private object _sendLock = new object();
    private bool _isSending = false;


    #region Host Peer Management

    public void StartHost(int listenPort)
    {
        if (_isHostMode)
        {
            Debug.LogWarning("Already in host mode!");
            return;
        }

        // 호스트 모드에서는 먼저 데디케이트 서버에 연결되어있는지 확인해야함.
        // if (!_isClientConnected)
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
    private void OnPeerAcceptCallback(IAsyncResult ar)
    {
        Socket peer = null;
        bool accepted = false;
        int peerId = 0;
        try
        {
            if (_peerListener == null)
            {
                return;
            }

            peer = _peerListener.EndAccept(ar); // Accept 작업 결과 가져오는 메서드

            lock (_peerLock)
            {
                if (_peerSessions.Count < maxPeers)
                {
                    peerId = Interlocked.Increment(ref _nextPeerId); // 락보다 성능 좋음. (찾아보니까 c++의 atomic과 비슷한 역할)
                    PeerSession session = new PeerSession
                    {
                        PeerId = peerId,
                        Socket = peer,
                        RecvBuffer = new RecvBuffer(BUFFER_SIZE)
                    };

                    _peerSessions[peerId] = session;
                    accepted = true;
                }
            }

            if (!accepted)
            {
                Debug.LogWarning($"Room full. reject peer={peer.RemoteEndPoint}");
                try { peer.Close(); } catch { }
                return;
            }

            Debug.Log($"Peer connected. peerId={peerId}, endpoint={peer.RemoteEndPoint}");
            // 피어 수신 시작
            RegisterPeerRecv(peerId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnPeerAcceptCallback failed: {ex.Message}");
            if (peer != null)
            {
                try { peer.Close(); } catch { }
            }
        }
        finally
        {
            // 성공/거절/예외와 무관하게 다음 accept를 항상 등록
            try
            {
                _peerListener?.BeginAccept(OnPeerAcceptCallback, null);
            }
            catch
            {
                // ignore
            }
        }
    }

    #region Peer Connect
    public void ConnectToHost(string ip, int port)
    {
        if (_isClientConnected)
        {
            Debug.LogWarning("Already connected to host!");
            return;
        }
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _hostSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _hostSocket.BeginConnect(endPoint, OnHostConnectCallback, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ConnectToHost failed: {ex.Message}");
        }
    }
    private void OnHostConnectCallback(IAsyncResult ar)
    {
        try
        {
            _hostSocket.EndConnect(ar);
            _isClientConnected = true;
            Debug.Log("Connected to host!");
            RegisterHostRecv();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnHostConnect failed: {ex.Message}");
        }
    }
    private void RegisterHostRecv()
    {
        if (!_isClientConnected) return;
        ArraySegment<byte> segment = _clientRecvBuffer.GetWriteSegment();
        _hostSocket.BeginReceive(segment.Array, segment.Offset, segment.Count, SocketFlags.None, OnHostRecvCallback, null);
    }
    private void OnHostRecvCallback(IAsyncResult ar)
    {
        try
        {
            int bytesRead = _hostSocket.EndReceive(ar);
            if (bytesRead == 0)
            {
                DisconnectFromHost("Host closed connection");
                return;
            }
            if (!_clientRecvBuffer.OnWrite(bytesRead))
            {
                DisconnectFromHost("RecvBuffer overflow");
                return;
            }
            int processedBytes = ProcessHostPackets();
            if (processedBytes < 0 || !_clientRecvBuffer.OnRead(processedBytes))
            {
                DisconnectFromHost("Packet processing failed");
                return;
            }
            _clientRecvBuffer.Clean();
            RegisterHostRecv();
        }
        catch (Exception ex)
        {
            DisconnectFromHost($"Host recv error: {ex.Message}");
        }
    }
    private int ProcessHostPackets()
    {
        int processedBytes = 0;
        while (true)
        {
            int dataSize = _clientRecvBuffer.DataSize - processedBytes;
            if (dataSize < PacketHeader.HeaderSize) break;
            ArraySegment<byte> buffer = _clientRecvBuffer.GetReadSegment();
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);
            if (dataSize < header.size) break;
            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize, packetData, 0, packetData.Length);
            MainThreadDispatcher.Enqueue(() =>
            {
                // 클라이언트가 호스트로부터 받은 패킷 처리
                PacketHandler.Instance.HandlePacket((PacketId)header.id, packetData);
            });
            processedBytes += header.size;
        }
        return processedBytes;
    }
    public void DisconnectFromHost(string reason)
    {
        if (!_isClientConnected) return;
        _isClientConnected = false;
        Debug.Log($"Disconnected from host: {reason}");
        _hostSocket?.Close();
        _hostSocket = null;
    }
    #endregion

    #region Peer Recv
    private void RegisterPeerRecv(int peerId)
    {
        PeerSession session = null;
        lock (_peerLock)
        {
            if (!_peerSessions.TryGetValue(peerId, out session))
            {
                Debug.LogWarning($"RegisterPeerRecv failed: peerId {peerId} not found");
                return;
            }
                
        }

        ArraySegment<byte> segment = session.RecvBuffer.GetWriteSegment();
        // PeerId를 통해 어떤 피어인지 특정.
        session.Socket.BeginReceive(segment.Array, segment.Offset, segment.Count, SocketFlags.None, OnPeerRecvCallback, peerId);
    }

    private void OnPeerRecvCallback(IAsyncResult ar)
    {
        Debug.Log("OnPeerRecvCallback called");
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
    #endregion

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
            {
                Debug.Log($"[PeerNetManager] Not enough data for header: dataSize={dataSize}");
                break;
            }

            ArraySegment<byte> buffer = session.RecvBuffer.GetReadSegment();
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);

            Debug.Log($"[PeerNetManager] header.size={header.size}, header.id={header.id}, dataSize={dataSize}");

            if (dataSize < header.size)
            {
                Debug.Log($"[PeerNetManager] Not enough data for full packet: header.size={header.size}, dataSize={dataSize}");
                break;
            }

            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize,
                packetData, 0, packetData.Length);

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[PeerNetManager] HandlePeerPacket called: peerId={peerId}, packetId={header.id}, dataLen={packetData.Length}");
                try
                {
                    PeerPacketHandler.Instance.HandlePeerPacket(peerId, (PacketId)header.id, packetData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PeerNetManager] HandlePeerPacket exception: {ex}");
                }
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

    #region Client To Peer Send
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

            ArraySegment<byte> packetSegment = new ArraySegment<byte>(sendBuffer);
            Send(packetSegment);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SendPacket Error: {ex.Message}");
        }
    }

    private void Send(ArraySegment<byte> packet)
    {
        if (!_isClientConnected || _hostSocket == null)
        {
            Debug.LogWarning("Cannot send: not connected to host");
            return;
        }

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

    private void RegisterSend()
    {
        if (!_isClientConnected || _hostSocket == null)
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
            _hostSocket.BeginSend(segments, SocketFlags.None, OnSendCallback, segments);
            Debug.Log($"Initiated send of {segments.Length} segments");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
            DisconnectFromHost("Send error");
        }
    }

    private void OnSendCallback(IAsyncResult ar)
    {
        try
        {
            if (_hostSocket == null) return;
            int bytesSent = _hostSocket.EndSend(ar);

            if (bytesSent == 0)
            {
                DisconnectFromHost("Send failed");
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
            DisconnectFromHost("Send callback error");
        }
    }

    #endregion


    #region Host Broadcast
    public void BroadcastToPeers(int senderPeerId, PacketId packetId, IMessage packet, bool includeSender = false)
    {
        byte[] sendBuffer = BuildPacketBuffer(packetId, packet);

        List<PeerSession> targets = new List<PeerSession>();
        lock (_peerLock)
        {
            foreach (var kv in _peerSessions)
            {
                if (!includeSender && kv.Key == senderPeerId)
                    continue;

                targets.Add(kv.Value);
            }
        }

        foreach (PeerSession session in targets)
        {
            TrySendToPeer(session, sendBuffer);
        }
    }

    private byte[] BuildPacketBuffer(PacketId packetId, IMessage packet)
    {
        byte[] body = packet.ToByteArray();
        byte[] buffer = new byte[4 + body.Length];

        Array.Copy(BitConverter.GetBytes((ushort)(4 + body.Length)), 0, buffer, 0, 2);
        Array.Copy(BitConverter.GetBytes((ushort)packetId), 0, buffer, 2, 2);
        Array.Copy(body, 0, buffer, 4, body.Length);

        return buffer;
    }

    private void TrySendToPeer(PeerSession session, byte[] sendBuffer)
    {
        try
        {
            session.Socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, OnPeerSendCallback, session.PeerId);
        }
        catch (Exception ex)
        {
            DisconnectPeer(session.PeerId, $"Peer send register error: {ex.Message}");
        }
    }

    private void OnPeerSendCallback(IAsyncResult ar)
    {
        int peerId = (int)ar.AsyncState;
        PeerSession session = null;

        lock (_peerLock)
        {
            _peerSessions.TryGetValue(peerId, out session);
        }

        if (session == null)
            return;

        try
        {
            int bytesSent = session.Socket.EndSend(ar);
            if (bytesSent <= 0)
            {
                DisconnectPeer(peerId, "Peer send failed");
            }
        }
        catch (Exception ex)
        {
            DisconnectPeer(peerId, $"Peer send callback error: {ex.Message}");
        }
    }
    #endregion
}
