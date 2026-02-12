using System;
using System.Collections;
using System.Collections.Generic;
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
}
