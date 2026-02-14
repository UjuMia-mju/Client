using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;
using UnityEngine;

public class NetManager : Singleton<NetManager>
{
    private Socket _socket;
    private bool _isConnected = false;
    private const int BUFFER_SIZE = 65536; // 64KB

    // Recv 버퍼
    private RecvBuffer _recvBuffer = new RecvBuffer(BUFFER_SIZE);

    // 송신 큐
    private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    private object _sendLock = new object();
    private bool _isSending = false;

    public bool IsConnected => _isConnected;


    #region Connect
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
    public void Send(ArraySegment<byte> packet)
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

    //---------------

    // ==================== 높은 수준의 전송 메서드들 ====================

    public void SendLogin(string userId, string password)
    {
        C_LOGIN loginPacket = new C_LOGIN
        {
            UserId = userId,
            Psw = password
        };

        SendPacket(PacketId.PKT_C_LOGIN, loginPacket);
    }

    public void SendEnterGame(ulong playerIndex)
    {
        C_ENTER_GAME enterGamePacket = new C_ENTER_GAME
        {
            PlayerIndex = playerIndex
        };

        SendPacket(PacketId.PKT_C_ENTER_GAME, enterGamePacket);
    }

    public void SendChat(string message)
    {
        C_CHAT chatPacket = new C_CHAT
        {
            Msg = message
        };

        SendPacket(PacketId.PKT_C_CHAT, chatPacket);
    }

    /// <summary>
    /// 핵심: 프로토콜 메시지를 패킷으로 변환하고 Send 호출
    /// </summary>
    private void SendPacket<T>(PacketId packetId, T packet) where T : IMessage
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
}
