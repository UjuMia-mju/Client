using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Google.Protobuf;
using Protocol;
using UnityEngine;

public delegate void PacketReceivedHandler(PacketId packetId, byte[] data);

public class BaseNetSession
{
    protected Socket _socket; // dedicate
    protected Socket _relaySocket;
    protected bool _isConnected = false;
    /// <summary>비동기 BeginConnect가 끝나기 전까지 true. 재연결 루프에서 중복 Connect 방지용.</summary>
    protected bool _connectAttemptInProgress;
    bool _shutdownRequested;
    public bool IsConnected => _isConnected;
    public bool HasPendingConnect => _connectAttemptInProgress;
    protected const int BUFFER_SIZE = 65536; // 64KB

    protected RecvBuffer _recvBuffer;

    protected Queue<ArraySegment<byte>> _sendQueue = new();
    protected object _sendLock = new object();
    protected bool _isSending = false;
    
    
    public event PacketReceivedHandler OnPacketReceivedEvent;
    public Action OnDisconnected;
    /// <summary>TCP 연결이 맺어진 직후(메인 스레드에서 호출).</summary>
    public Action OnConnectedToServer;

    // 생성자: RecvBuffer 초기화 추가
    public BaseNetSession()
    {
        _recvBuffer = new RecvBuffer(BUFFER_SIZE);
    }

    // base connect, disconnect, send, receive implementation
    #region Connect
    void CloseSocketSilently()
    {
        var socket = _socket;
        _socket = null;
        if (socket == null)
            return;

        try
        {
            if (socket.Connected)
                socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BaseNetSession] Socket shutdown: {ex.Message}");
        }

        try
        {
            socket.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BaseNetSession] Socket close: {ex.Message}");
        }
    }

    /// <summary>연결 중·연결됨 여부와 관계없이 소켓과 송수신 상태를 정리합니다.</summary>
    public void ForceShutdown(bool raiseDisconnected = false)
    {
        _shutdownRequested = true;
        bool wasConnected = _isConnected;
        _isConnected = false;
        _connectAttemptInProgress = false;

        lock (_sendLock)
        {
            _isSending = false;
            _sendQueue.Clear();
        }

        CloseSocketSilently();

        if (!raiseDisconnected || !wasConnected)
            return;

        try
        {
            MainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke());
        }
        catch
        {
            // 플레이 모드 종료 직후 디스패처가 없을 수 있음
        }
    }

    public void Connect(string ip, int port)
    {
        if (_isConnected)
        {
            Debug.LogWarning("Already connected!");
            return;
        }

        if (_connectAttemptInProgress)
            return;

        CloseSocketSilently();
        _shutdownRequested = false;

        try
        {
            if (_recvBuffer == null)
                _recvBuffer = new RecvBuffer(BUFFER_SIZE);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _connectAttemptInProgress = true;
            // register async connect callback
            _socket.BeginConnect(endPoint, OnConnectCallback, null);

        }
        catch (Exception ex)
        {
            _connectAttemptInProgress = false;
            CloseSocketSilently();
            Debug.LogError($"Connect failed: {ex.Message}");
        }
    }

    // 연결 성공하면 .NET에서 콜백 호출
    private void OnConnectCallback(IAsyncResult ar)
    {
        if (_shutdownRequested)
            return;

        try
        {
            _socket?.EndConnect(ar);
            _isConnected = true;
            _connectAttemptInProgress = false;
            RegisterRecv();
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log("ConnectedServer!");
                OnConnectedToServer?.Invoke();
            });
        }
        catch (Exception ex)
        {
            _connectAttemptInProgress = false;
            Debug.LogError($"OnConnect failed: {ex.Message}");
            CloseSocketSilently();
        }
    }

    public void ConnectToRelayServer(string ip, int port)
    {
    }

    private void OnRelayConnectCallback(IAsyncResult ar)
    {
    } 
    
    #endregion

    #region send
    protected virtual void Send(ArraySegment<byte> packet)
    {
        bool needRegisterSend = false;
        if (!_isConnected)
        {
            Debug.LogWarning("Cannot send: not connected");
            return;
        }

        lock (_sendLock)
        {
            _sendQueue.Enqueue(packet);

            if (!_isSending)
            {
                _isSending = true;
                needRegisterSend = true;
            }
        }

        if (needRegisterSend)
        {
            RegisterSend();
        }
    }

    protected virtual void RegisterSend()
    {
        if (!_isConnected)
        {
            return;
        }
            
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
            _socket.BeginSend(segments, SocketFlags.None, OnSendCallback, segments);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
            Disconnect("Send error");
        }
    }

    protected virtual void OnSendCallback(IAsyncResult ar)
    {
        if (_shutdownRequested)
            return;

        bool needRegisterSend = false;
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
                    Debug.Log($"Sent {bytesSent} bytes, { _sendQueue.Count} packets left in queue");
                    needRegisterSend = true;
                }
                else
                {
                    _isSending = false;
                }
            }
            if (needRegisterSend)
            {
                RegisterSend();
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

            // packet size (header + data)
            Array.Copy(BitConverter.GetBytes((ushort)(4 + packetData.Length)), 0, sendBuffer, 0, 2);

            // packet id
            Array.Copy(BitConverter.GetBytes((ushort)packetId), 0, sendBuffer, 2, 2);

            // packet data
            Array.Copy(packetData, 0, sendBuffer, 4, packetData.Length);

            // ArraySegment로 변환해서 Send 호출
            ArraySegment<byte> packet_segment = new ArraySegment<byte>(sendBuffer);
            Send(packet_segment);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SendPacket Error: {ex.Message}");
        }
    }


    #endregion

    #region Recv

    protected virtual void RegisterRecv()
    {
        if (!_isConnected)
        {
            return;
        }
        if (_recvBuffer == null)
        {
            _recvBuffer = new RecvBuffer(BUFFER_SIZE);
        }

        ArraySegment<byte> segment = _recvBuffer.GetWriteSegment();
        _socket.BeginReceive(segment.Array, segment.Offset, segment.Count, SocketFlags.None, OnRecvCallback, null);
    }

    protected virtual void OnRecvCallback(IAsyncResult ar)
    {
        if (_shutdownRequested)
            return;

        try
        {
            int bytesRead = _socket.EndReceive(ar);

            if (bytesRead == 0)
            {
                Disconnect("Remote closed connection");
                return;
            }

            if (!_recvBuffer.OnWrite(bytesRead))
            {
                Disconnect("RecvBuffer overflow");
                return;
            }

            // packet Processing
            int processedBytes = ProcessPackets();

            if (processedBytes < 0 || !_recvBuffer.OnRead(processedBytes))
            {
                Disconnect("Packet processing failed");
                return;
            }

            _recvBuffer.Clean();
            RegisterRecv();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnRecv failed: {ex.Message}");
            Disconnect("Receive error");
        }
    }

    protected virtual int ProcessPackets()
    {
        int processedBytes = 0;

        while (true)
        {
            int dataSize = _recvBuffer.DataSize - processedBytes;
            // 패킷 헤더 크기보다 데이터가 적으면 처리 중단
            if (dataSize < PacketHeader.HeaderSize)
            {
                break;
            }
                
            // 패킷 헤더 읽기
            ArraySegment<byte> buffer = _recvBuffer.GetReadSegment();
            // header parsing
            PacketHeader header = PacketHeader.FromBytes(buffer.Array, buffer.Offset + processedBytes);

            if (dataSize < header.size || header.size <= 0)
            {
                break;
            }
                
            byte[] packetData = new byte[header.size - PacketHeader.HeaderSize];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes + PacketHeader.HeaderSize, packetData, 0, packetData.Length);

            // 진단 로그: 어떤 인스턴스에서 발생하는지, 이벤트 구독자 수 확인
            int subscriberCount = 0;
            try
            {
                // 리플렉션으로 이벤트에 연결된 델리게이트 확인
                FieldInfo evtField = typeof(BaseNetSession).GetField("OnPacketReceivedEvent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (evtField != null)
                {
                    Delegate d = evtField.GetValue(this) as Delegate;
                    subscriberCount = d?.GetInvocationList().Length ?? 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BaseNetSession] Failed to inspect subscribers: {ex}");
            }

            OnPacketReceivedEvent?.Invoke((PacketId)header.id, packetData);
            processedBytes += header.size;
        }

        return processedBytes;
    }

    #endregion

    #region Disconnect Handler
    public virtual void Disconnect(string reason)
    {
        if (!_isConnected && !_connectAttemptInProgress && _socket == null)
            return;

        Debug.Log($"Disconnected: {reason}");
        ForceShutdown(raiseDisconnected: _isConnected);
    }
    #endregion
}
