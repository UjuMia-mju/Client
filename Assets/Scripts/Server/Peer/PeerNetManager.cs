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

public class PeerNetManager : Singleton<PeerNetManager>
{
    // 호스트가 피어로부터 받은 패킷을 처리하는 클래스
    // NetManager의 피어 receive 루프에서 호출됨
    // 각 C_ 패킷에 대한 이벤트를 정의하여 게임 로직에서 구독할 수 있도록 함
    // 예: 플레이어 이동, 채팅 메시지, 애니메이션 상태 변경, 아이템 상호작용 등
    // ================ Host Mode (Peer 관리) ================
    private const int BUFFER_SIZE = 65536; // 64KB -> netManager의 버퍼 사이즈와 동일하게 설정함.
    private bool _isHostMode = false;
    public bool IsHostMode => _isHostMode;
    // 호스트가 상위 데디케이트 서버에 붙는 소켓은 기존 netmanager의 _socket 사용
    // 하위 피어를 받기 위한 리스너/피어 목록은 별도로 사용함
    private Socket _peerListener;
    private int _nextPeerId = 0;
    private readonly int maxPeers = 3; // 최대 피어 수 (호스트 포함 총 4명)
    private readonly object _peerLock = new object();
    // 연결된 peer session 관리
    private Dictionary<int, PeerSession> _peerSessions = new Dictionary<int, PeerSession>();

    #region Host Peer Management

    public void StartHost(int listenPort)
    {
        if (_isHostMode)
        {
            Debug.LogWarning("Already in host mode!");
            return;
        }

        // 호스트 모드에서는 먼저 데디케이트 서버에 연결되어있는지 확인해야함.
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
