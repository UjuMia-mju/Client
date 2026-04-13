using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using Protocol;
using UnityEngine;

/// <summary>
/// 호스트의 네트워크 매니저 클래스
/// - 호스트 모드에서 피어 연결 수락 및 패킷 수신 처리
/// - 피어에게 패킷 브로드캐스트 기능 제공
/// </summary>
public class HostNetManager
{
    private static HostNetManager _instance;
    public static HostNetManager Instance => _instance ??= new HostNetManager();

    protected Socket _socket;
    protected object _sendLock = new object();
    protected const int BUFFER_SIZE = 65536;
    private bool _isHostMode = false;
    public bool IsHostMode => _isHostMode;
    protected object _hostLock = new object();
    private const int maxPeers = 4; // 최대 피어 수 (호스트 포함 총 5명)
    private int _nextPeerId = 0;
    private Dictionary<int, PeerSession> _peerSessions = new ();

    #region Host Peer Management

    public void StartHost(int listenPort)
    {
        if (_isHostMode)
        {
            Debug.LogWarning("Already in host mode!");
            return;
        }

        // 호스트 모드에서는 먼저 데디케이트 서버에 연결되어있는지 확인해야함. - > 일단은 연결 안해도 호스트 모드로 진입 가능하게 해놓음. 나중에 필요하면 수정.
        // if (!_isClientConnected)
        // {
        //     Debug.LogWarning("Must connect to upstream server before starting host mode!");
        //     return;
        // }

        // 하위 Peer 수신 리스너 시작
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, listenPort)); // 모든 인터페이스에서 listenPort로 바인딩 (즉, 다 열어준다는 뜻입니다.)
            _socket.Listen(6); // 최대 6명까지 대기열 허용 (혹시 몰라 넉넉하게)
            _socket.BeginAccept(OnPeerAcceptCallback, null); // 비동기로 Accept 시작

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
            if (_socket == null)
            {
                return;
            }

            peer = _socket.EndAccept(ar); // Accept 작업 결과 가져오는 메서드

            lock (_sendLock)
            {
                if (_peerSessions.Count < maxPeers)
                {
                    peerId = Interlocked.Increment(ref _nextPeerId); // 찾아보니까 c++의 atomic과 비슷한 역할
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
                _socket?.BeginAccept(OnPeerAcceptCallback, null);
            }
            catch
            {
                // ignore
            }
        }
    }
    #endregion

    #region Peer Recv
    private void RegisterPeerRecv(int peerId)
    {
        PeerSession session = null;
        lock (_hostLock)
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
        int peerId = (int)ar.AsyncState;

        PeerSession session = null;
        lock (_hostLock)
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
        lock (_sendLock)
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
                break;
            }

            ArraySegment<byte> buffer = session.RecvBuffer.GetReadSegment();
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);

            if (dataSize < header.size)
            {
                break;
            }

            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize,
                packetData, 0, packetData.Length);

            MainThreadDispatcher.Enqueue(() =>
            {
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
    #endregion

    private void DisconnectPeer(int peerId, string reason)
    {
        PeerSession session = null;

        lock (_sendLock)
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

    #region Broadcast To Peers

    public void BroadcastToPeers(int senderPeerId, PacketId packetId, IMessage packet, bool includeSender = true)
    {
        byte[] sendBuffer = BuildPacketBuffer(packetId, packet);

        List<PeerSession> targets = new ();
        
        lock (_sendLock)
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

        lock (_sendLock)
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

    public void SendToPeer(int peerId, PacketId packetId, IMessage packet)
    {
        byte[] sendBuffer = BuildPacketBuffer(packetId, packet);

        PeerSession session = null;
        lock (_sendLock)
        {
            _peerSessions.TryGetValue(peerId, out session);
        }

        if (session != null)
        {
            TrySendToPeer(session, sendBuffer);
        }
    }
}

