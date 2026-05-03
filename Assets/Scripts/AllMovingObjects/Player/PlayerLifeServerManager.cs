using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Protocol;

public class PlayerLifeServerManager : MonoBehaviorSingleton<PlayerLifeServerManager>
{
    private const float RESPAWN_DELAY = 15f;

    private readonly Dictionary<ulong, Coroutine> respawnCoroutines = new Dictionary<ulong, Coroutine>();

    void Start()
    {
        if (PeerPacketHandler.Instance != null)
            PeerPacketHandler.Instance.OnPeerPlayerDeadEvent += OnPeerPlayerDead;
    }

    void Update()
    {
        if (PeerPacketHandler.Instance != null)
            PeerPacketHandler.Instance.OnPeerPlayerDeadEvent -= OnPeerPlayerDead;
    }

    private void OnPeerPlayerDead(int peerId, C_PLAYER_DEAD packet)
    {
        OnReceivePlayerDead(packet.PlayerId);
    }

    /// <summary>단일 진입점. PlayManager.OnPeerPlayerDead 또는 호스트 자신의 PlayerStat에서 직접 호출.</summary>
    public void OnReceivePlayerDead(ulong playerId)
    {
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
        {
            Debug.Log("[PlayerLifeServerManager] 비호스트는 OnReceivePlayerDead 호출 무시.");
            return;
        }

        if (respawnCoroutines.ContainsKey(playerId))
        {
            Debug.Log($"[PlayerLifeServerManager] 이미 부활 대기 중: playerId={playerId}");
            return;
        }

        Debug.Log($"[PlayerLifeServerManager] 사망 확정 → 브로드캐스트 + {RESPAWN_DELAY}s 후 부활. playerId={playerId}");

        PacketSender.Instance.BroadcastPlayerDead(playerId);

        // 호스트는 자기 broadcast echo를 못 받으므로 로컬도 직접 dispatch.
        // (이 한 줄이 핵심 — 그래야 호스트 자신의 PlayerStat 또는 OtherPlayers에 적용됨)
        if (PlayManager.Instance != null)
            PlayManager.Instance.ApplyPlayerDeadLocally(playerId);

        Coroutine co = StartCoroutine(RespawnPlayerAfterDelay(playerId));
        respawnCoroutines[playerId] = co;
    }

    private IEnumerator RespawnPlayerAfterDelay(ulong playerId)
    {
        yield return new WaitForSeconds(RESPAWN_DELAY);
        respawnCoroutines.Remove(playerId);

        (Vector3 pos, Quaternion rot) = PlayManager.Instance != null
            ? PlayManager.Instance.GetSpawnPoseForPlayer(playerId)
            : (Vector3.zero, Quaternion.identity);

        Debug.Log($"[PlayerLifeServerManager] 부활 브로드캐스트. playerId={playerId}, pos={pos}");
        PacketSender.Instance.BroadcastPlayerRevive(playerId, pos, rot);

        // 호스트 본인의 echo 부재 보정
        if (PlayManager.Instance != null)
            PlayManager.Instance.ApplyPlayerReviveLocally(playerId, pos, rot);

        if (HostStatManager.Instance != null && HostStatManager.Instance.TryGetPlayerStat(playerId, out var stat))
        {
            stat.ChangeData(stat.statData.maxHp, 1f);
            PacketSender.Instance.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        }
    }
}
