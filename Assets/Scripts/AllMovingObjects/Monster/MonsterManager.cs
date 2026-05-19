using UnityEngine;
using System.Collections.Generic;
using Protocol;

public class MonsterManager : MonoBehaviour
{
    [System.Serializable]
    public class MonsterPrefabEntry
    {
        public Monsters monsterKey;
        public GameObject prefab;
    }

    [SerializeField] private List<MonsterPrefabEntry> monsterPrefabs = new List<MonsterPrefabEntry>();

    private Dictionary<int, GameObject> monsterDic = new Dictionary<int, GameObject>();


    private int nextMonsterId = 1;

    public static MonsterManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 호스트 진입점: 로컬 스폰 + id 부여 + 패킷 송신은 ServerManager에 위임
    public void SpawnMonster(Monsters monsterKey, Vector3 pos, Quaternion rot)
    {
        if (!ConnectManager.Instance.isHost) return;

        GameObject prefab = FindPrefabByKey(monsterKey);
        if (prefab == null) return;

        int newId = nextMonsterId++;
        GameObject spawned = Instantiate(prefab, pos, rot);
        AssignMonsterId(spawned, newId);
        monsterDic[newId] = spawned;

        // ServerManager 거치지 않고 PacketSender 직접 호출 (Resource 패턴)
        S_MONSTER_SPAWN packet = new S_MONSTER_SPAWN
        {
            MonsterId = newId,
            MonsterKey = (int)monsterKey,
            Pos = new PosInfo { X = pos.x, Y = pos.y, Z = pos.z },
            Rot = new RotInfo { X = rot.x, Y = rot.y, Z = rot.z, W = rot.w }
        };
        PacketSender.Instance.BroadcastMonsterSpawn(packet);
    }

    public void MonsterDead(int id)
    {
        if (!ConnectManager.Instance.isHost) return;

        if (!monsterDic.ContainsKey(id)) return;

        GameObject spawned = monsterDic[id];
        monsterDic.Remove(id);
        if (spawned != null) Destroy(spawned);

        S_MONSTER_DEAD packet = new S_MONSTER_DEAD { MonsterId = id };
        PacketSender.Instance.BroadcastMonsterDead(packet);
    }

    // 피어 진입점: ServerManager가 패킷 수신 후 호출
    public void SpawnFromNetwork(Monsters monsterKey, int id, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = FindPrefabByKey(monsterKey);
        if (prefab == null) return;

        GameObject spawned = Instantiate(prefab, pos, rot);
        AssignMonsterId(spawned, id);
        monsterDic[id] = spawned;
    }

    // [추가] 피어 진입점: ServerManager가 사망 패킷 수신 후 호출
    public void DestroyFromNetwork(int id)
    {
        if (!monsterDic.ContainsKey(id)) return;

        GameObject spawned = monsterDic[id];
        monsterDic.Remove(id);
        if (spawned != null) Destroy(spawned);
    }

    // [추가] 피어 진입점: 호스트로부터 받은 애니메이션 상태(int)를 해당 몬스터에 적용
    public void UpdateAnimStateFromNetwork(int id, int stateInt)
    {
        if (!monsterDic.TryGetValue(id, out GameObject spawned) || spawned == null)
            return;

        // DesertWorm 처리
        DesertWormAnimator wormAnim = spawned.GetComponent<DesertWormAnimator>();
        if (wormAnim != null)
        {
            wormAnim.SetState((WormAnimState)stateInt);
            return;
        }

        // 다른 몬스터 종류 애니메이터도 여기서 분기 추가
    }

    // 피어 진입점: 호스트로부터 받은 위치/회전을 해당 몬스터에 적용
    public void UpdateTransformFromNetwork(int id, Vector3 pos, Quaternion rot)
    {
        if (!monsterDic.TryGetValue(id, out GameObject spawned) || spawned == null)
            return;
        spawned.transform.SetPositionAndRotation(pos, rot);
    }

    private GameObject FindPrefabByKey(Monsters targetKey)
    {
        if (targetKey == Monsters.None) return null;

        foreach (MonsterPrefabEntry entry in monsterPrefabs)
        {
            if (entry == null) continue;
            if (entry.monsterKey == targetKey)
                return entry.prefab;
        }
        return null;
    }

    // 새 몬스터 종류 추가 시 분기 2줄만 추가
    private void AssignMonsterId(GameObject spawned, int id)
    {
        if (spawned == null) return;

        DesertWorm worm = spawned.GetComponent<DesertWorm>();
        if (worm != null) { worm.monsterId = id; return; }

        // BossWorm boss = spawned.GetComponent<BossWorm>();
        // if (boss != null) { boss.monsterId = id; return; }
    }
}
