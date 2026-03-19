using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Protocol;
using UnityEngine;

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
    // 호스트가 상위 데디케이트 서버에 붙는 소켓은 기존 _socket 사용
    // 하위 피어를 받기 위한 리스너/피어 목록은 별도로 사용합니다.
    private Socket _peerListener;
    private int _nextPeerId = 1;
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
