using System.Collections.Generic;
using Protocol;
using UnityEngine;
using System.Linq;

public class PlayManager : SceneSingleton<PlayManager>
{
    //[SerializeField] private GameObject localPlayerPrefab;
    [SerializeField] private GameObject remotePlayerPrefab;

    private GameObject _localPlayer;
    public Dictionary<ulong, GameObject> _remotePlayers = new();

    private SpaceshipAssembly spaceshipAssembly;
    void Start()
    {
        spaceshipAssembly = FindFirstObjectByType<SpaceshipAssembly>();


        PeerPacketHandler.Instance.OnPeerEnterGameEvent += OnPeerEnterGame;
        PeerPacketHandler.Instance.OnPeerMoveEvent += OnPeerMove;
        PeerPacketHandler.Instance.OnPeerAnimationEvent += OnPeerAnimation;
        PeerPacketHandler.Instance.OnPeerItemAttachedEvent += OnPeerItemPickup;
        PeerPacketHandler.Instance.OnPeerItemDetachedEvent += OnPeerItemDetach;
        PeerPacketHandler.Instance.OnPeerObjectSpawnEvent += OnPeerObjectSpawn;
        PeerPacketHandler.Instance.OnPeerObjectDestroyEvent += OnPeerObjectDestroy;
        PeerPacketHandler.Instance.OnPeerSpaceshipInsertEvent += OnPeerSpaceshipInsert;

        HostPacketHandler.Instance.OnPlayerEnterEvent += OnServerPlayerEnter;
        HostPacketHandler.Instance.OnMoveEvent += OnHostMove;
        HostPacketHandler.Instance.OnAnimationEvent += OnHostAnimation;
        HostPacketHandler.Instance.OnItemAttached += OnHostItemPickup;
        HostPacketHandler.Instance.OnItemDetatched += OnHostItemDetach;
        HostPacketHandler.Instance.OnItemMoveEvent += OnHostItemMove;
        HostPacketHandler.Instance.OnStatEvent += OnHostStat;
        HostPacketHandler.Instance.OnObjectSpawnEvent += OnHostObjectSpawn;
        HostPacketHandler.Instance.OnObjectDestroyEvent += OnHostObjectDestroy;
        HostPacketHandler.Instance.OnSpaceshipUpdateEvent += OnHostSpaceshipUpdate;
        HostPacketHandler.Instance.OnSpaceshipCompleteEvent += OnHostSpaceshipComplete;
        HostPacketHandler.Instance.OnTimerSyncEvent += OnHostTimerSync;

        PacketHandler.Instance.OnEnterGameResultEvent += OnEnterGameResult;

        // PacketHandler.Instance.OnAnimationEvent += OnAnim;
        //PacketHandler.Instance.OnStatEvent += OnPlayerStat;
        //PacketHandler.Instance.OnItemAttached += OnItemAttached;
        //PacketHandler.Instance.OnItemDetatched += OnItemDetatched;
        //PacketHandler.Instance.OnItemMoveEvent += OnItemMove;
        //PacketHandler.Instance.OnCraftTableEvent += OnCraftTableItemInstantiate;

        // 서버에 ENTER_GAME 패킷 전송 (게임 입장 요청)
        //PacketHandler.Instance.SendEnterGame((ulong)NetManager.Instance._playerId);
        // 로컬 플레이어 생성
        //SpawnLocalPlayer();



        // 스탯 관련
        //HostPacketHandler.Instance.OnStatEvent += PeerStatManager.Instance.OnStatChanged;
    }

    // HACK : 애니메이션은 실시간으로 처리해야되기때문에 Update에서 처리도록 했습니다. 올바른 처리일까요?
    private void Update()
    {
        

    }

    void OnDestroy()
    {
        PeerPacketHandler.Instance.OnPeerEnterGameEvent -= OnPeerEnterGame;
        PeerPacketHandler.Instance.OnPeerMoveEvent -= OnPeerMove;
        PeerPacketHandler.Instance.OnPeerAnimationEvent -= OnPeerAnimation;
        PeerPacketHandler.Instance.OnPeerItemAttachedEvent -= OnPeerItemPickup;
        PeerPacketHandler.Instance.OnPeerItemDetachedEvent -= OnPeerItemDetach;
        PeerPacketHandler.Instance.OnPeerObjectSpawnEvent -= OnPeerObjectSpawn;
        PeerPacketHandler.Instance.OnPeerObjectDestroyEvent -= OnPeerObjectDestroy;
        PeerPacketHandler.Instance.OnPeerSpaceshipInsertEvent -= OnPeerSpaceshipInsert;

        HostPacketHandler.Instance.OnPlayerEnterEvent -= OnServerPlayerEnter;
        HostPacketHandler.Instance.OnMoveEvent -= OnHostMove;
        HostPacketHandler.Instance.OnAnimationEvent -= OnHostAnimation;
        HostPacketHandler.Instance.OnItemAttached -= OnHostItemPickup;
        HostPacketHandler.Instance.OnItemDetatched -= OnHostItemDetach;
        HostPacketHandler.Instance.OnItemMoveEvent -= OnHostItemMove;
        HostPacketHandler.Instance.OnStatEvent -= OnHostStat;
        HostPacketHandler.Instance.OnObjectSpawnEvent -= OnHostObjectSpawn;
        HostPacketHandler.Instance.OnObjectDestroyEvent -= OnHostObjectDestroy;
        HostPacketHandler.Instance.OnSpaceshipUpdateEvent -= OnHostSpaceshipUpdate;
        HostPacketHandler.Instance.OnSpaceshipCompleteEvent -= OnHostSpaceshipComplete;
        HostPacketHandler.Instance.OnTimerSyncEvent -= OnHostTimerSync;

        PacketHandler.Instance.OnEnterGameResultEvent -= OnEnterGameResult;
    }

    //private void SpawnLocalPlayer()
    //{
    //    _localPlayer = Instantiate(localPlayerPrefab, SpawnOffset.transform.position, Quaternion.identity);
    //    _localPlayer.name = "LocalPlayer";
    //}

    // 다음과 같이 명명합니다 - 서버에 들어가는 함수 제외하고
    // OnHost기능명, OnPeer기능명
    #region 인게임 호스트 -> 피어 패킷

    // 피어가 호스트로부터 받은 S_PLAYER_ENTER 패킷 처리 (호스트가 피어에게 보낸 패킷)
    private void OnServerPlayerEnter(S_PLAYER_ENTER packet)
    {
        OnPlayerEnter((int)packet.Player.PlayerId, packet);
    }

    // Move 패킷 처리
    private void OnHostMove(ulong playerId, S_MOVE packet)
    {
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
                Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);

                remotePlayer.SetTargetPosition(pos, rot);
            }
        }
        else
        {
            Debug.LogWarning($"[HostMove] Received move for unknown player: {playerId}");
        }
    }

    private void OnHostAnimation(ulong playerId, S_PLAYER_ANIMATION packet)
    {
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                remotePlayer.SetAnimState(packet.State);
            }
        }
        else
        {
            Debug.LogWarning($"[HostAnimation] Received move for unknown player: {playerId}");
        }
    }

    private void OnHostItemPickup(ulong playerId, S_OBJECT_PICKUP packet)
    {
        // 아이템을 집는 패킷 처리 로직
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
                if (item != null)
                    remotePlayer.SetEquipItem(item);
                else
                    Debug.LogWarning($"[HostItemPickup] Item not found: {packet.ObjectId.ItemId}");
            }
        }
        else
        {
            Debug.LogWarning($"[HostItemPickup] Received move for unknown player: {playerId}");
        }
    }

    private void OnHostItemDetach(ulong playerId, S_OBJECT_DROP packet)
    {
        // 아이템을 빼는 패킷 처리 로직
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                remotePlayer.DetachEquipItem();
            }
        }
        else
        {
            Debug.LogWarning($"[HostItemPickup] Received move for unknown player: {playerId}");
        }
    }

    private void OnHostItemMove(S_OBJECT_MOVE packet)
    {
        Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
        if (item != null)
        {
            Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
            Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
            item.SetPos(pos, rot);
        }
        else
        {
            Debug.LogWarning($"[HostItemMove] Item not found: {packet.ObjectId.ItemId}");
        }
    }

    private void OnHostStat(S_PLAYER_STAT packet)
    {
        if (_remotePlayers.TryGetValue(packet.PlayerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
                remotePlayer.SetStat(packet.Hp, packet.Oxygen);
        }
    }

    private void OnHostObjectSpawn(S_OBJECT_SPAWN packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
        ItemManager.Instance.SpawnItemFromNetwork(packet.ItemId, packet.ItemStringKey, pos, rot);
    }

    private void OnHostObjectDestroy(S_OBJECT_DESTROY packet)
    {
        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null)
        {
            Debug.LogWarning($"[OnHostObjectDestroy] itemId={packet.ItemId}에 해당하는 아이템을 찾을 수 없습니다.");
            return;
        }

        ItemManager.Instance.UnregisterItem(item);
        Destroy(item.gameObject);
        Debug.Log($"[PlayManager] ObjectDestroy 처리: id={packet.ItemId}");
    }

    private void OnHostSpaceshipUpdate(S_SPACESHIP_UPDATE packet)
    {
        if (spaceshipAssembly == null) return;
        spaceshipAssembly.SyncIndex(packet.CurrentIndex);
    }

    private void OnHostSpaceshipComplete(S_SPACESHIP_COMPLETE packet)
    {
        GameRuleManager.Instance.ReturnToStageSelectScene(packet.Success);
    }
    private void OnHostTimerSync(S_TIMER_SYNC packet)
    {
        GameRuleManager.Instance.SyncTimer(packet.RemainingTime);
    }

    #endregion

    #region 인게임 피어 -> 호스트 패킷 

    // 호스트가 피어로부터 받은 C_TEST_ENTER_GAME 패킷 처리
    private void OnPeerEnterGame(int peerId, C_TEST_ENTER_GAME packet)
    {
        Debug.Log($"Peer {peerId} entered the game with name:");
        // Peer 구조에서 입장 패킷을 받았을 때 S_PLAYER_ENTER를 직접 만들어서 OnPlayerEnter 호출
        S_PLAYER_ENTER enterPacket = new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = peerId,
                Name = "Peer", // packet.Name이 null이면 기본값
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        };
        OnPlayerEnter(peerId, enterPacket);
    }

    private void OnPeerMove(int peerId, C_MOVE packet)
    {
        ulong playerId = (ulong)peerId;

        // 내 플레이어는 무시 (이미 로컬에서 움직임)
        // if (playerId == (ulong)NetManager.Instance._playerId)
        //     return;

        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
                Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);

                remotePlayer.SetTargetPosition(pos, rot);
            }
        }
        else
        {
            Debug.LogWarning($"[PeerMove] Received move for unknown player: {playerId}");
        }
    }

    private void OnPeerAnimation(int peerId, C_PLAYER_ANIMATION packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                remotePlayer.SetAnimState(packet.State);
            }
        }
        else
        {
            Debug.LogWarning($"[HostMove] Received move for unknown player: {peerId}");
        }
    }

    private void OnPeerItemPickup(int peerId, C_OBJECT_PICKUP packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
                if (item != null)
                    remotePlayer.SetEquipItem(item);
                else
                    Debug.LogWarning($"[PeerItemPickup] Item not found: {packet.ObjectId.ItemId}");
            }
        }
        else
        {
            Debug.LogWarning($"[PeerItemPickup] Unknown player: {peerId}");
        }
    }

    private void OnPeerItemDetach(int playerId, C_OBJECT_DROP packet)
    {
        // 아이템을 빼는 패킷 처리 로직
        if (_remotePlayers.TryGetValue((ulong)playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                remotePlayer.DetachEquipItem();
            }
        }
        else
        {
            Debug.LogWarning($"[HostItemPickup] Received move for unknown player: {playerId}");
        }
    }

    private void OnPeerObjectMove(int playerId, C_OBJECT_MOVE packet)
    {
        Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
        if (item != null)
        {
            Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
            Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
            item.SetPos(pos, rot);
        }
        else
        {
            Debug.LogWarning($"[PeerObjectMove] Item not found: {packet.ObjectId.ItemId}");
        }
    }

    // 핸들러 추가
    private void OnPeerStat(int peerId, S_PLAYER_STAT packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
                remotePlayer.SetStat(packet.Hp, packet.Oxygen);
        }
    }

    // 호스트가 피어로부터 받은 C_OBJECT_SPAWN 패킷 처리
    private void OnPeerObjectSpawn(int peerId, C_OBJECT_SPAWN packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
        ItemManager.Instance.SpawnItemFromNetwork(0, packet.ItemStringKey, pos, rot);
    }

    // 호스트가 피어로부터 받은 C_OBJECT_DESTROY 패킷 처리
    private void OnPeerObjectDestroy(int peerId, C_OBJECT_DESTROY packet)
    {
        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null)
        {
            Debug.LogWarning($"[OnPeerObjectDestroy] itemId={packet.ItemId}에 해당하는 아이템을 찾을 수 없습니다.");
            return;
        }

        ItemManager.Instance.UnregisterItem(item);
        Destroy(item.gameObject);
        Debug.Log($"[PlayManager] 피어 요청으로 ObjectDestroy: id={packet.ItemId}");
    }



    private void OnPeerSpaceshipInsert(int peerId, C_SPACESHIP_INSERT packet)
    {
        if (spaceshipAssembly == null)
        {
            Debug.LogWarning("[OnPeerSpaceshipInsert] SpaceshipAssembly를 찾을 수 없습니다.");
            return;
        }

        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null)
        {
            Debug.LogWarning($"[OnPeerSpaceshipInsert] itemStringKey={packet.ItemStringKey}에 해당하는 아이템을 찾을 수 없습니다.");
            return;
        }

        spaceshipAssembly.AddTargetItems(item.gameObject);
    }


    #endregion

    // 게임 입장 성공
    private void OnEnterGameResult(S_ENTER_GAME packet)
    {
        if (packet.Success)
        {
            Debug.Log("✓ Successfully entered the game!");
        }
        else
        {
            Debug.LogError("✗ Failed to enter game!");
        }
    }

    // 기존 플레이어 목록 수신 (게임 입장 시)
    private void OnPlayerList(S_PLAYER_LIST packet)
    {
        Debug.Log($"📋 Received player list: {packet.Players.Count} players");

        foreach (var playerInfo in packet.Players)
        {
            SpawnRemotePlayer(playerInfo);
        }
    }

    // 새 플레이어 입장
    private void OnPlayerEnter(int peerId, S_PLAYER_ENTER packet)
    {
        Debug.Log($"👤 Player {packet.Player.Name} entered!");
        SpawnRemotePlayer(packet.Player);
    }

    // 플레이어 퇴장
    private void OnPlayerLeave(S_PLAYER_LEAVE packet)
    {
        Debug.Log($"👋 Player {packet.Player.PlayerId} left!");
        RemoveRemotePlayer((ulong)packet.Player.PlayerId);
    }



    // HACK : 이전 이동 로직, 다른 상황에 대비해 주석화하고 새로 개발한 Peer 및 Host 용 로직으로 대체하고 테스트합니다.
    // 플레이어 이동
    //private void OnPlayerMove(S_MOVE packet)
    //{
    //    // 내 플레이어는 무시 (이미 로컬에서 움직임)
    //    if (packet.PlayerId == (ulong)NetManager.Instance._playerId)
    //    {
    //        return;
    //    }
            

    //    if (_remotePlayers.TryGetValue(packet.PlayerId, out GameObject playerObj))
    //    {
    //        // 기존 플레이어 위치 업데이트
    //        OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
    //        if (remotePlayer != null)
    //        {
    //            Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
    //            Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);

    //            remotePlayer.SetTargetPosition(pos, rot);
    //        }
    //    }
    //    else
    //    {
    //        // 아직 생성되지 않은 플레이어 (패킷 순서 문제)
    //        Debug.LogWarning($"Received move for unknown player: {packet.PlayerId}");
    //    }
    //}

    private void SpawnRemotePlayer(PlayerGameInfo playerInfo)
    {
        ulong id = (ulong)playerInfo.PlayerId;
        if (_remotePlayers.ContainsKey(id))
        {
            Debug.LogWarning($"Player {playerInfo.PlayerId} already exists!");
            return;
        }

        // 스폰 위치 고정 (테스트용: 0, 23, 2)
        Vector3 pos = new Vector3(0, 23, 2);
        Quaternion rot = playerInfo.Rot != null ? new Quaternion(playerInfo.Rot.X, playerInfo.Rot.Y, playerInfo.Rot.Z, playerInfo.Rot.W) : Quaternion.identity;

        GameObject playerObj = Instantiate(remotePlayerPrefab, pos, rot);

        //GameObject playerObj = Instantiate(remotePlayerPrefab, SpawnOffset.transform.position, rot);

        playerObj.name = $"RemotePlayer_{playerInfo.PlayerId}";
        
        OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
        if (remotePlayer != null)
        {
            remotePlayer.PlayerId = id;
            remotePlayer.PlayerName = playerInfo.Name;
        }

        _remotePlayers[id] = playerObj;

        Debug.Log($"✓ Spawned remote player: {playerInfo.Name} ({playerInfo.PlayerId})");
    }

    private void RemoveRemotePlayer(ulong playerId)
    {
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            Destroy(playerObj);
            _remotePlayers.Remove(playerId);
            Debug.Log($"✓ Removed remote player: {playerId}");
        }
    }

}
