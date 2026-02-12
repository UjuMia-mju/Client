using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
            byte[] packetData = new byte[header.size];
            Array.Copy(buffer.Array, buffer.Offset + processedBytes, packetData, 0, header.size);

            // MainThreadDispatcher.Enqueue(() =>
            // {
            //     PacketManager.Instance.HandlePacket(packetData);
            // });

            processedBytes += header.size;
        }

        return processedBytes;
    }

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
