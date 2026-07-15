using UnityEngine;
using Protocol;

public class MonsterServerManager : MonoBehaviour
{
    private void Start()
    {
        if (!ConnectManager.Instance.isHost && HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnMonsterSpawnEvent += OnSpawnReceived;
            HostPacketHandler.Instance.OnMonsterDeadEvent += OnDeadReceived;
            HostPacketHandler.Instance.OnMonsterAnimationEvent += OnAnimationReceived;
            HostPacketHandler.Instance.OnMonsterMoveEvent += OnMoveReceived;
            HostPacketHandler.Instance.OnMonsterHitEvent += OnHitReceived;
        }
    }

    private void OnDestroy()
    {
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnMonsterSpawnEvent -= OnSpawnReceived;
            HostPacketHandler.Instance.OnMonsterDeadEvent -= OnDeadReceived;
            HostPacketHandler.Instance.OnMonsterAnimationEvent -= OnAnimationReceived;
            HostPacketHandler.Instance.OnMonsterMoveEvent -= OnMoveReceived;
            HostPacketHandler.Instance.OnMonsterHitEvent -= OnHitReceived;
        }
    }

    private void OnSpawnReceived(S_MONSTER_SPAWN packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);

        MonsterManager.Instance.SpawnFromNetwork((Monsters)packet.MonsterKey, packet.MonsterId, pos, rot);
    }

    private void OnDeadReceived(S_MONSTER_DEAD packet)
    {
        MonsterManager.Instance.DestroyFromNetwork(packet.MonsterId);
    }

    // [추가] 호스트가 보내준 몬스터 애니메이션 상태를 적용
    private void OnAnimationReceived(S_MONSTER_ANIMATION packet)
    {
        // proto: monster_id, state  → C#: MonsterId, State
        MonsterManager.Instance.UpdateAnimStateFromNetwork(packet.MonsterId, packet.State);
    }

    private void OnMoveReceived(S_MONSTER_MOVE packet)
    {
        Vector3 pos = new Vector3(packet.Pos.X, packet.Pos.Y, packet.Pos.Z);
        Quaternion rot = new Quaternion(packet.Rot.X, packet.Rot.Y, packet.Rot.Z, packet.Rot.W);
        MonsterManager.Instance.UpdateTransformFromNetwork(packet.MonsterId, pos, rot);
    }

    private void OnHitReceived(S_MONSTER_HIT packet)
    {
        MonsterManager.Instance.ApplyDamageFromNetwork(packet.MonsterId, packet.Damage);
    }
}
