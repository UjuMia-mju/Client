using System.Collections.Generic;
using Protocol;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PlayManager : SceneSingleton<PlayManager>
{
    [Header("인게임 시작 동기화 (스테이지 선택 → S_GAME_READY_TO_START 후 이 씬)")]
    [Tooltip("인스펙터에 직접 연결하세요. 멀티 시작 동기화 시 카운트다운 UI에 사용됩니다.")]
    [SerializeField] private ReadyToStartPanelController inGameReadyPanel;

    [SerializeField] private GameObject remotePlayerPrefab;

    [Header("Spawn Points (입장 순서대로 인덱스 배정)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("스폰 포인트가 부족할 때 fallback 위치")]
    [SerializeField] private Vector3 fallbackSpawnPos = new Vector3(0, 100, 2);

    private GameObject _localPlayer;
    public Dictionary<ulong, GameObject> _remotePlayers = new();

    private SpaceshipAssembly spaceshipAssembly;

    void Start()
    {
        BootstrapInGameReadyPanelIfNeeded();

        spaceshipAssembly = FindFirstObjectByType<SpaceshipAssembly>();

        PeerPacketHandler.Instance.OnPeerEnterGameEvent += OnPeerEnterGame;
        PeerPacketHandler.Instance.OnPeerMoveEvent += OnPeerMove;
        PeerPacketHandler.Instance.OnPeerAnimationEvent += OnPeerAnimation;
        PeerPacketHandler.Instance.OnPeerItemAttachedEvent += OnPeerItemPickup;
        PeerPacketHandler.Instance.OnPeerItemDetachedEvent += OnPeerItemDetach;
        PeerPacketHandler.Instance.OnPeerObjectSpawnEvent += OnPeerObjectSpawn;
        PeerPacketHandler.Instance.OnPeerObjectDestroyEvent += OnPeerObjectDestroy;
        PeerPacketHandler.Instance.OnPeerSpaceshipInsertEvent += OnPeerSpaceshipInsert;
        PeerPacketHandler.Instance.OnPeerResourceHitEvent += OnPeerResourceHit;
        PeerPacketHandler.Instance.OnPeerPlayerDeadEvent += OnPeerPlayerDead;
        PeerPacketHandler.Instance.OnPeerMushroomExplodeEvent += OnPeerMushroomExplode;

        HostPacketHandler.Instance.OnEnterGameEvent += OnHostEnterGame;
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
        HostPacketHandler.Instance.OnResourceSpawnEvent += OnHostResourceSpawn;
        HostPacketHandler.Instance.OnResourceDestroyEvent += OnHostResourceDestroy;
        HostPacketHandler.Instance.OnPlayerDeadEvent += OnHostPlayerDead;
        HostPacketHandler.Instance.OnPlayerReviveEvent += OnHostPlayerRevive;
        HostPacketHandler.Instance.OnMushroomExplodeEvent += OnHostMushroomExplode;

        var ph = PacketHandler.Instance;
        if (ph != null)
            ph.OnRoomMemberLeaveEvent += OnRoomMemberLeftInGame;

        var localPlayer = FindFirstObjectByType<Player>();
        if (localPlayer != null)
        {
            var (pos, rot) = ResolveSpawnPose((ulong)NetManager.Instance._playerId, new PlayerGameInfo());
            localPlayer.transform.SetPositionAndRotation(pos, rot);
        }
    }

    /// <summary>
    /// 서버 시작 시각까지 남은 시간을 씬 로드 이후에 맞추기 위해 보관된 S_GAME_READY_TO_START(또는 폴백)로
    /// 인게임에서만 카운트다운합니다.
    /// </summary>
    void BootstrapInGameReadyPanelIfNeeded()
    {
        if (!GameplayReadyCoordinator.TryTakePendingForUi(
                out S_GAME_READY_TO_START serverPacket,
                out bool useFallback,
                out IReadOnlyList<ulong> fallbackIds,
                out int fallbackDelay))
            return;

        var panel = inGameReadyPanel;

        void OnCountdownDone()
        {
            GameplayReadyCoordinator.NotifyGateReleased();
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        if (panel == null)
        {
            Debug.LogWarning(
                "[PlayManager] 게임 시작 동기화 데이터가 있으나 inGameReadyPanel이 비어 있습니다. PlayManager에 ReadyToStartPanelController를 연결하세요. 게이트만 해제합니다.");
            OnCountdownDone();
            return;
        }

        panel.WarmUpBindings();

        if (serverPacket != null)
            panel.BeginFromPacket(serverPacket, OnCountdownDone);
        else if (useFallback)
            panel.BeginFallback(fallbackIds, fallbackDelay, OnCountdownDone);
        else
            OnCountdownDone();
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
        PeerPacketHandler.Instance.OnPeerResourceHitEvent -= OnPeerResourceHit;
        PeerPacketHandler.Instance.OnPeerPlayerDeadEvent -= OnPeerPlayerDead;
        PeerPacketHandler.Instance.OnPeerMushroomExplodeEvent -= OnPeerMushroomExplode;

        HostPacketHandler.Instance.OnEnterGameEvent -= OnHostEnterGame;
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
        HostPacketHandler.Instance.OnResourceSpawnEvent -= OnHostResourceSpawn;
        HostPacketHandler.Instance.OnResourceDestroyEvent -= OnHostResourceDestroy;
        HostPacketHandler.Instance.OnPlayerDeadEvent -= OnHostPlayerDead;
        HostPacketHandler.Instance.OnPlayerReviveEvent -= OnHostPlayerRevive;
        HostPacketHandler.Instance.OnMushroomExplodeEvent -= OnHostMushroomExplode;

        var ph = PacketHandler.Instance;
        if (ph != null)
            ph.OnRoomMemberLeaveEvent -= OnRoomMemberLeftInGame;
        // [추가] 구독 이전에 도착해 유실되었을 수 있는 C_ENTER_GAME을 재처리.
        //       재진입 시 피어 씬이 호스트보다 먼저 로드되면 호스트의
        //       PlayManager가 구독하기 전에 C_ENTER_GAME이 도착해 호스트가
        //       피어를 스폰하지 못하는 race를 방지한다.
        PeerPacketHandler.Instance.ReplayPendingEnterGames();
    }

    private void Update() { }

    private void OnPeerMushroomExplode(int peerId, byte[] payload)
    {
        _ = peerId;
        if (payload == null || payload.Length == 0)
            return;

        MushroomExplosionSyncBus.Publish(payload);
        MushroomExplosionSyncBus.BroadcastExplodeFromHost(payload);
    }

    private void OnHostMushroomExplode(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
            return;

        MushroomExplosionSyncBus.Publish(payload);
    }

    #region 호스트 → 피어 수신

    /// <summary>피어: S_ENTER_GAME으로 전체 플레이어 목록 받아 일괄 스폰.</summary>
    private void OnHostEnterGame(S_ENTER_GAME packet)
    {
        Debug.Log($"[PlayManager] S_ENTER_GAME 수신. players={packet.Players.Count}");
        foreach (var playerInfo in packet.Players)
            SpawnRemotePlayer(playerInfo);
    }

    /// <summary>서버 브로드캐스트(S_MOVE 등)에는 내 플레이어도 포함될 수 있으며, 로컬은 Scene의 Player가 처리함.</summary>
    static bool IsNetworkPacketForLocalPlayer(ulong playerId)
        => NetManager.Instance != null && playerId == NetManager.Instance._playerId;

    private void OnHostMove(ulong playerId, S_MOVE packet)
    {
        if (IsNetworkPacketForLocalPlayer(playerId))
            return;

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
        else Debug.LogWarning($"[HostMove] unknown player: {playerId}");
    }

    private void OnHostAnimation(ulong playerId, S_PLAYER_ANIMATION packet)
    {
        if (IsNetworkPacketForLocalPlayer(playerId))
            return;

        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
                remotePlayer.SetAnimState(packet.State);
        }
        else Debug.LogWarning($"[HostAnimation] unknown player: {playerId}");
    }

    private void OnHostItemPickup(ulong playerId, S_OBJECT_PICKUP packet)
    {
        if (IsNetworkPacketForLocalPlayer(playerId))
            return;

        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
                if (item != null) remotePlayer.SetEquipItem(item);
                else Debug.LogWarning($"[HostItemPickup] Item not found: {packet.ObjectId.ItemId}");
            }
        }
        else Debug.LogWarning($"[HostItemPickup] unknown player: {playerId}");
    }

    private void OnHostItemDetach(ulong playerId, S_OBJECT_DROP packet)
    {
        if (IsNetworkPacketForLocalPlayer(playerId))
            return;

        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null) remotePlayer.DetachEquipItem(packet.Charged);
        }
        else Debug.LogWarning($"[HostItemDetach] unknown player: {playerId}");
    }

    private void OnHostItemMove(S_OBJECT_MOVE packet)
    {
        Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
        if (item != null)
        {
            item.SetPos(
                new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z),
                new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W));
        }
        else Debug.LogWarning($"[HostItemMove] Item not found: {packet.ObjectId.ItemId}");
    }

    private void OnHostStat(S_PLAYER_STAT packet)
    {
        if (IsNetworkPacketForLocalPlayer(packet.PlayerId))
            return;

        if (_remotePlayers.TryGetValue(packet.PlayerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null) remotePlayer.SetStat(packet.Hp, packet.Oxygen);
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
        if (item == null) { Debug.LogWarning($"[OnHostObjectDestroy] itemId={packet.ItemId} not found."); return; }

        Player localPlayer = FindFirstObjectByType<Player>();
        if (localPlayer != null && localPlayer.playerItemSystem.currentEquipItem == item.gameObject)
        {
            localPlayer.isPlayerGetSomething = false;
            localPlayer.playerItemSystem.DetachItem();
        }

        ItemManager.Instance.UnregisterItem(item);
        Destroy(item.gameObject);
        Debug.Log($"[PlayManager] ObjectDestroy: id={packet.ItemId}");
    }

    private void OnHostSpaceshipUpdate(S_SPACESHIP_UPDATE packet)
    {
        if (spaceshipAssembly == null) return;
        spaceshipAssembly.SyncMission(packet.ItemStringKeyMission, packet.CurrentIndex);
    }

    private void OnHostSpaceshipComplete(S_SPACESHIP_COMPLETE packet)
    {
        var stars = 0;
        if (packet.Success)
        {
            var assembly = FindFirstObjectByType<SpaceshipAssembly>();
            if (assembly != null)
                stars = assembly.GetFilledStarCountForStageClear();
        }

        GameRuleManager.Instance.ReturnToStageSelectScene(packet.Success, stars);
    }

    private void OnHostTimerSync(S_TIMER_SYNC packet)
        => GameRuleManager.Instance.SyncTimer(packet.RemainingTime);

    private void OnHostResourceSpawn(S_RESOURCE_SPAWN packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        ResourceManager.Instance.ApplyResourceIdFromNetwork(packet.ResourceId, packet.ResourceStringKey, pos);
    }

    private void OnHostResourceDestroy(S_RESOURCE_DESTROY packet)
    {
        ResourceManager.Instance.DestroyResourceFromNetwork(packet.ResourceId);
    }

    private void OnHostPlayerDead(S_PLAYER_DEAD packet)
        => ApplyPlayerDeadLocally(packet.PlayerId);

    private void OnHostPlayerRevive(S_PLAYER_REVIVE packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
        ApplyPlayerReviveLocally(packet.PlayerId, pos, rot);
    }

    /// <summary>
    /// 호스트는 자기 자신의 broadcast echo를 받지 않으므로,
    /// PlayerLifeServerManager가 broadcast 직후 이 메서드를 직접 호출해 로컬에도 적용한다.
    /// 피어는 S_PLAYER_DEAD 수신 → OnHostPlayerDead 경로로 같은 메서드를 탄다.
    /// </summary>
    public void ApplyPlayerDeadLocally(ulong playerId)
    {
        Debug.Log($"[PlayManager] ApplyPlayerDeadLocally. playerId={playerId}");

        if (playerId == NetManager.Instance._playerId)
        {
            var localPlayer = FindFirstObjectByType<Player>();
            if (localPlayer != null)
            {
                var stat = localPlayer.GetComponent<PlayerStat>();
                if (stat != null) stat.ApplyDeathFromNetwork();
            }
        }
        else
        {
            if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
            {
                var op = playerObj.GetComponent<OtherPlayers>();
                if (op != null) op.ApplyDeath();
            }
            else Debug.LogWarning($"[PlayManager] ApplyPlayerDeadLocally: unknown remote player {playerId}");
        }
    }

    public void ApplyPlayerReviveLocally(ulong playerId, Vector3 pos, Quaternion rot)
    {
        Debug.Log($"[PlayManager] ApplyPlayerReviveLocally. playerId={playerId}, pos={pos}");

        if (playerId == NetManager.Instance._playerId)
        {
            var localPlayer = FindFirstObjectByType<Player>();
            if (localPlayer != null)
            {
                var stat = localPlayer.GetComponent<PlayerStat>();
                if (stat != null) stat.ApplyReviveFromNetwork(pos, rot);
            }
        }
        else
        {
            if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
            {
                var op = playerObj.GetComponent<OtherPlayers>();
                if (op != null) op.ApplyRevive(pos, rot);
            }
            else Debug.LogWarning($"[PlayManager] ApplyPlayerReviveLocally: unknown remote player {playerId}");
        }
    }

    #endregion

    #region 피어 → 호스트 수신

    /// <summary>
    /// 호스트: 피어 입장 처리.
    /// S_ENTER_GAME 브로드캐스트는 PeerPacketHandler.HandlePeerEnterGame에서 이미 완료.
    /// 여기서는 호스트 측 스폰 + 신규 피어에게 기존 피어 목록 추가 전달.
    /// </summary>
    private void OnPeerEnterGame(int peerId, C_ENTER_GAME packet)
    {
        Debug.Log($"[PlayManager] Peer {peerId} entered.");
        S_PLAYER_ENTER enterPacket = new S_PLAYER_ENTER
        {
            Player = new PlayerGameInfo
            {
                PlayerId = peerId,
                Name = RoomMemberDisplayCache.GetDisplayNameOrFallback((ulong)peerId, "Peer"),
                Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
            }
        };
        SpawnRemotePlayer(enterPacket.Player);

        // 신규 피어에게 기존 피어들 정보 추가 전달 (PeerPacketHandler의 S_ENTER_GAME에 포함되지 않은 경우 대비)
        foreach (var existingId in _remotePlayers.Keys)
        {
            if ((int)existingId == peerId) continue;
            PacketSender.Instance.BroadcastToPeers(PacketId.PKT_S_PLAYER_ENTER, new S_PLAYER_ENTER
            {
                Player = new PlayerGameInfo
                {
                    PlayerId = (int)existingId,
                    Name = RoomMemberDisplayCache.GetDisplayNameOrFallback(existingId, "Peer"),
                    Pos = new PosInfo { X = 0, Y = 0, Z = 0 },
                    Rot = new RotInfo { X = 0, Y = 0, Z = 0, W = 1 }
                }
            });
        }
    }

    private void OnPeerMove(int peerId, C_MOVE packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
                remotePlayer.SetTargetPosition(
                    new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z),
                    new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W));
        }
        else Debug.LogWarning($"[PeerMove] unknown player: {peerId}");
    }

    private void OnPeerAnimation(int peerId, C_PLAYER_ANIMATION packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null) remotePlayer.SetAnimState(packet.State);
        }
        else Debug.LogWarning($"[PeerAnimation] unknown player: {peerId}");
    }

    private void OnPeerItemPickup(int peerId, C_OBJECT_PICKUP packet)
    {
        if (_remotePlayers.TryGetValue((ulong)peerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null)
            {
                Items item = ItemManager.Instance.GetItem((int)packet.ObjectId.ItemId);
                if (item != null) remotePlayer.SetEquipItem(item);
                else Debug.LogWarning($"[PeerItemPickup] Item not found: {packet.ObjectId.ItemId}");
            }
        }
        else Debug.LogWarning($"[PeerItemPickup] unknown player: {peerId}");
    }

    private void OnPeerItemDetach(int playerId, C_OBJECT_DROP packet)
    {
        if (_remotePlayers.TryGetValue((ulong)playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null) remotePlayer.DetachEquipItem(packet.Charged);
        }
        else Debug.LogWarning($"[PeerItemDetach] unknown player: {playerId}");
    }

    private void OnPeerObjectSpawn(int peerId, C_OBJECT_SPAWN packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
        ItemManager.Instance.SpawnItemAndBroadcast(packet.ItemStringKey, pos, rot);
    }

    private void OnPeerObjectDestroy(int peerId, C_OBJECT_DESTROY packet)
    {
        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null) { Debug.LogWarning($"[OnPeerObjectDestroy] itemId={packet.ItemId} not found."); return; }
        ItemManager.Instance.UnregisterItem(item);
        Destroy(item.gameObject);
        Debug.Log($"[PlayManager] 피어 요청 ObjectDestroy: id={packet.ItemId}");
    }

    private void OnPeerSpaceshipInsert(int peerId, C_SPACESHIP_INSERT packet)
    {
        if (spaceshipAssembly == null) { Debug.LogWarning("[OnPeerSpaceshipInsert] SpaceshipAssembly not found."); return; }
        Items item = ItemManager.Instance.GetItem(packet.ItemId);
        if (item == null) { Debug.LogWarning($"[OnPeerSpaceshipInsert] itemId={packet.ItemId} not found."); return; }
        spaceshipAssembly.AddTargetItems(item.gameObject);
    }

    private void OnPeerPlayerDead(int peerId, C_PLAYER_DEAD packet)
    {
        Debug.Log($"[PlayManager] 피어 사망 보고 수신: peerId={peerId}, playerId={packet.PlayerId}");
        PlayerLifeServerManager.Instance.OnReceivePlayerDead(packet.PlayerId);
    }

    #endregion

    private void OnPlayerEnter(int peerId, S_PLAYER_ENTER packet)
    {
        Debug.Log($"👤 Player {packet.Player.Name} entered!");
        SpawnRemotePlayer(packet.Player);
    }

    private void OnRoomMemberLeftInGame(S_ROOM_MEMBER_LEAVE packet)
    {
        if (packet == null) return;
        Debug.Log($"[PlayManager] Room member left (ingame despawn): playerId={packet.PlayerId}");
        RemoveRemotePlayer(packet.PlayerId);
    }

    private void SpawnRemotePlayer(PlayerGameInfo playerInfo)
    {
        ulong id = (ulong)playerInfo.PlayerId;

        if (id == (ulong)NetManager.Instance._playerId)
        {
            Debug.Log("내 플레이어는 스폰하지 않습니다.");
            return;
        }

        if (_remotePlayers.ContainsKey(id))
        {
            if (_remotePlayers.TryGetValue(id, out GameObject existingObj))
            {
                OtherPlayers existing = existingObj.GetComponent<OtherPlayers>();
                if (existing != null)
                {
                    existing.PlayerName = playerInfo.Name;
                    existing.SetNicknameDisplay(playerInfo.Name);
                }
            }
            return; // S_PLAYER_ENTER 직후 S_ENTER_GAME 등에서 동일 플레이어가 재전달되는 경우 허용
        }

        (Vector3 pos, Quaternion rot) = ResolveSpawnPose(id, playerInfo);

        GameObject playerObj = Instantiate(remotePlayerPrefab, pos, rot);
        playerObj.name = $"RemotePlayer_{playerInfo.PlayerId}";

        OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
        if (remotePlayer != null)
        {
            remotePlayer.PlayerId = id;
            remotePlayer.PlayerName = playerInfo.Name;
            remotePlayer.SetNicknameDisplay(playerInfo.Name);
        }

        _remotePlayers[id] = playerObj;
        Debug.Log($"✓ Spawned remote player: {playerInfo.Name} ({playerInfo.PlayerId}) @ {pos}");
    }

    /// <summary>
    /// 스폰 위치 결정 우선순위:
    /// 1) RoomMembershipTracker의 입장 순서로 spawnPoints 인덱스 배정
    /// 2) 패킷에 의미 있는 위치가 들어있으면 그 위치
    /// 3) fallbackSpawnPos
    /// </summary>
    private (Vector3 pos, Quaternion rot) ResolveSpawnPose(ulong playerId, PlayerGameInfo playerInfo)
    {
        // 1) 입장 순서 기반 인덱스
        var ordered = RoomMembershipTracker.Instance?.OrderedIds;
        if (ordered != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = -1;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == playerId) { idx = i; break; }
            }

            if (idx >= 0 && idx < spawnPoints.Length && spawnPoints[idx] != null)
                return AlignPoseToGravity(spawnPoints[idx].position, spawnPoints[idx].rotation, spawnPoints[idx]);
        }

        // 2) 패킷이 (0,0,0)이 아니면 그 값을 사용
        if (playerInfo.Pos != null &&
            (playerInfo.Pos.X != 0f || playerInfo.Pos.Y != 0f || playerInfo.Pos.Z != 0f))
        {
            Vector3 pktPos = new Vector3(playerInfo.Pos.X, playerInfo.Pos.Y, playerInfo.Pos.Z);
            Quaternion pktRot = playerInfo.Rot != null
                ? new Quaternion(playerInfo.Rot.X, playerInfo.Rot.Y, playerInfo.Rot.Z, playerInfo.Rot.W)
                : Quaternion.identity;
            return (pktPos, pktRot);
        }

        // 3) fallback
        return (fallbackSpawnPos, Quaternion.identity);
    }

    private (Vector3 pos, Quaternion rot) AlignPoseToGravity(Vector3 pos, Quaternion rot, Transform referenceTransform)
    {
        PlanetGravity planet = FindFirstObjectByType<PlanetGravity>();
        if (planet == null || referenceTransform == null)
            return (pos, rot);

        Vector3 up = planet.GetGravityUp(referenceTransform);
        if (up.sqrMagnitude < 1e-6f)
            return (pos, rot);
        up.Normalize();

        Vector3 fwd = Vector3.ProjectOnPlane(rot * Vector3.forward, up);
        if (fwd.sqrMagnitude < 1e-4f)
            fwd = Vector3.ProjectOnPlane(rot * Vector3.right, up);
        if (fwd.sqrMagnitude < 1e-4f)
            return (pos, rot);
        fwd.Normalize();

        return (pos, Quaternion.LookRotation(fwd, up));
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

    public void UpdateRemotePlayerStat(ulong playerId, int hp, float oxygen)
    {
        if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            OtherPlayers remotePlayer = playerObj.GetComponent<OtherPlayers>();
            if (remotePlayer != null) remotePlayer.SetStat(hp, oxygen);
        }
    }

    /// <summary>
    /// 호스트가 OnNetworkReady()에서 즉시 브로드캐스트한 S_PLAYER_ENTER 처리.
    /// S_ENTER_GAME보다 먼저 도착하므로 S_PLAYER_ANIMATION보다 항상 선행 보장.
    /// </summary>
    private void OnServerPlayerEnter(S_PLAYER_ENTER packet)
    {
        SpawnRemotePlayer(packet.Player);
    }

    private void OnPeerResourceHit(int peerId, C_RESOURCE_HIT packet)
    {
        Debug.Log($"[PlayManager] 피어 자원 타격 수신: peerId={peerId}, resourceId={packet.ResourceId}");
        ResourceServerManager.Instance.OnReceiveHit(packet.ResourceId);
    }

    public (Vector3 pos, Quaternion rot) GetSpawnPoseForPlayer(ulong playerId)
    {
        return ResolveSpawnPose(playerId, new Protocol.PlayerGameInfo());
    }
}