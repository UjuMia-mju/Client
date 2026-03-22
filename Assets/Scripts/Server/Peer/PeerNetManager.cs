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
