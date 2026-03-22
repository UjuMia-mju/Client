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

// PeerNetManager는 클라이언트가 호스트에 접속할 때 사용되는 네트워크 매니저입니다.
public class PeerNetManager : BaseNetSession
{
    // singleton
    private static PeerNetManager _instance;
    public static PeerNetManager Instance => _instance ??= new PeerNetManager();
    public PeerNetManager()
    {
        // 이벤트 구독: 패킷 도착 시 HandlePeerPacket 호출
        this.OnPacketReceivedEvent += (packetId, data) => HandlePeerPacket(1, packetId, data);
    }

    private void HandlePeerPacket(int peerId, PacketId packetId, byte[] data)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            PeerPacketHandler.Instance.HandlePeerPacket(peerId, packetId, data);
        });
    }
}
