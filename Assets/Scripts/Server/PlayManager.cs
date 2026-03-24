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


    void Start()
    {
        PacketManager.Instance.OnPlayerListEvent += OnPlayerList;
        PacketManager.Instance.OnPlayerEnterEvent += OnPlayerEnter;
        PacketManager.Instance.OnPlayerLeaveEvent += OnPlayerLeave;
        PacketManager.Instance.OnMoveEvent += OnPlayerMove;
        PacketManager.Instance.OnEnterGameResultEvent += OnEnterGameResult;
        PacketManager.Instance.OnAnimationEvent += OnAnim;
        //PacketManager.Instance.OnStatEvent += OnPlayerStat;
        PacketManager.Instance.OnItemAttached += OnItemAttached;
        PacketManager.Instance.OnItemDetatched += OnItemDetatched;
        //PacketManager.Instance.OnItemMoveEvent += OnItemMove;
        //PacketManager.Instance.OnCraftTableEvent += OnCraftTableItemInstantiate;

        // 서버에 ENTER_GAME 패킷 전송 (게임 입장 요청)
        NetManager.Instance.SendEnterGame((ulong)NetManager.Instance._playerId);
        // 로컬 플레이어 생성
        //SpawnLocalPlayer();
    }

    // HACK : 애니메이션은 실시간으로 처리해야되기때문에 Update에서 처리하도록 했습니다. 올바른 처리일까요?
    private void Update()
    {
        
    }

    void OnDestroy()
    {
        PacketManager.Instance.OnPlayerListEvent -= OnPlayerList;
        PacketManager.Instance.OnPlayerEnterEvent -= OnPlayerEnter;
        PacketManager.Instance.OnPlayerLeaveEvent -= OnPlayerLeave;
        PacketManager.Instance.OnMoveEvent -= OnPlayerMove;
        PacketManager.Instance.OnEnterGameResultEvent -= OnEnterGameResult;
        PacketManager.Instance.OnAnimationEvent -= OnAnim;
        //PacketManager.Instance.OnStatEvent -= OnPlayerStat;
        PacketManager.Instance.OnItemAttached -= OnItemAttached;
        PacketManager.Instance.OnItemDetatched -= OnItemDetatched;
        //PacketManager.Instance.OnItemMoveEvent -= OnItemMove;
        //PacketManager.Instance.OnCraftTableEvent -= OnCraftTableItemInstantiate;
    }
    //private void SpawnLocalPlayer()
    //{
    //    _localPlayer = Instantiate(localPlayerPrefab, SpawnOffset.transform.position, Quaternion.identity);
    //    _localPlayer.name = "LocalPlayer";
    //}
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
    private void OnPlayerEnter(S_PLAYER_ENTER packet)
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

    // 플레이어 이동
    private void OnPlayerMove(S_MOVE packet)
    {
        if (PlayManager.Instance == null)
        {
            Debug.LogWarning("PlayManager instance is null. Cannot process move packet.");
            return;
        }
        // 내 플레이어는 무시 (이미 로컬에서 움직임)
        if (packet.PlayerId == (ulong)NetManager.Instance._playerId)
            return;

        if (_remotePlayers.TryGetValue(packet.PlayerId, out GameObject playerObj))
        {
            // 기존 플레이어 위치 업데이트
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
            // 아직 생성되지 않은 플레이어 (패킷 순서 문제)
            Debug.LogWarning($"Received move for unknown player: {packet.PlayerId}");
        }
    }

    private void SpawnRemotePlayer(PlayerGameInfo playerInfo)
    {
        ulong id = (ulong)playerInfo.PlayerId;
        if (_remotePlayers.ContainsKey(id))
        {
            Debug.LogWarning($"Player {playerInfo.PlayerId} already exists!");
            return;
        }

        Vector3 pos = playerInfo.Pos != null ? new Vector3(playerInfo.Pos.X, playerInfo.Pos.Y, playerInfo.Pos.Z) : Vector3.zero;
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
