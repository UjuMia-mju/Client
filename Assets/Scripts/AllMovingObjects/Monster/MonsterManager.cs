using UnityEngine;
using System.Collections.Generic;
using Protocol;

[DefaultExecutionOrder(-100)]
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

    // 피어가 호스트의 스폰 패킷을 받기 전까지 보관할 씬 배치 몬스터 목록
    private List<Monster> pendingScenePlacedMonsters = new List<Monster>();

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

        // 씬에 미리 배치된 몬스터를 일괄 수집/등록
        // [DefaultExecutionOrder(-100)] 로 다른 Monster 보다 먼저 실행되어야 함.
        Monster[] placed = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (Monster m in placed)
        {
            if (m == null) continue;
            if (!m.IsScenePlacedMonster) continue;
            RegisterScenePlacedMonster(m);
        }
    }

    // 씬 배치 몬스터 등록 - 호스트는 즉시 ID 부여 + 스폰 브로드캐스트, 피어는 보류 목록에 추가
    private void RegisterScenePlacedMonster(Monster monster)
    {
        if (monster == null) return;
        if (monster.MonsterKey == Monsters.None)
        {
            Debug.LogError($"[MonsterManager] 씬 배치 몬스터 '{monster.name}' 의 MonsterKey 가 None 입니다.", monster);
            return;
        }

        bool isHost = ConnectManager.Instance != null && ConnectManager.Instance.isHost;
        if (isHost)
        {
            int newId = nextMonsterId++;
            monster.monsterId = newId;
            monsterDic[newId] = monster.gameObject;

            // 애니/이동 패킷보다 먼저 도착해야 하므로 즉시 송신
            if (PacketSender.Instance != null)
            {
                Vector3 pos = monster.transform.position;
                Quaternion rot = monster.transform.rotation;
                S_MONSTER_SPAWN packet = new S_MONSTER_SPAWN
                {
                    MonsterId = newId,
                    MonsterKey = (int)monster.MonsterKey,
                    Pos = new PosInfo { X = pos.x, Y = pos.y, Z = pos.z },
                    Rot = new RotInfo { X = rot.x, Y = rot.y, Z = rot.z, W = rot.w }
                };
                PacketSender.Instance.BroadcastMonsterSpawn(packet);
                Debug.Log($"[MonsterManager] 씬 배치 몬스터 동기화: id={newId}, key={monster.MonsterKey}");
            }
            else
            {
                Debug.LogWarning($"[MonsterManager] PacketSender 미초기화 - 씬 배치 몬스터 스폰 브로드캐스트 실패: id={newId}");
            }
        }
        else
        {
            // 피어: 호스트의 SpawnFromNetwork 가 올 때 매칭해서 ID 부여
            pendingScenePlacedMonsters.Add(monster);
        }
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

        // 호스트는 DesertWorm.DieRoutine 가 dieAnimDuration 만큼 대기 후 본 메서드를 호출한다.
        // 따라서 호스트에서는 즉시 Destroy 해도 무방. (피어와 동일하게 위임해도 됨)
        if (spawned != null)
        {
            Monster monsterComp = spawned.GetComponent<Monster>();
            if (monsterComp != null) monsterComp.PlayDeathAndDestroy();
            else Destroy(spawned);
        }

        S_MONSTER_DEAD packet = new S_MONSTER_DEAD { MonsterId = id };
        PacketSender.Instance.BroadcastMonsterDead(packet);
    }

    // 피어 진입점: ServerManager가 패킷 수신 후 호출
    public void SpawnFromNetwork(Monsters monsterKey, int id, Vector3 pos, Quaternion rot)
    {
        // 1) 씬에 이미 배치된 동일 키 몬스터가 있으면 그것을 사용 (Items 패턴)
        Monster existing = FindPendingScenePlacedMonster(monsterKey, pos);
        if (existing != null)
        {
            existing.monsterId = id;
            monsterDic[id] = existing.gameObject;
            pendingScenePlacedMonsters.Remove(existing);

            // 호스트 측 권위 위치/회전으로 보정
            existing.transform.SetPositionAndRotation(pos, rot);

            Debug.Log($"[MonsterManager] 씬 배치 몬스터 ID 동기화 (피어): key={monsterKey}, id={id}");
            return;
        }

        // 2) 일반 네트워크 스폰
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

        if (spawned == null) return;

        // 피어 측에서는 Die 애니메이션을 재생할 시간을 확보한 뒤에 파괴해야 한다.
        // 몬스터별 연출(시간)을 알고 있는 Monster 컴포넌트가 처리하도록 위임.
        Monster monsterComp = spawned.GetComponent<Monster>();
        if (monsterComp != null) monsterComp.PlayDeathAndDestroy();
        else Destroy(spawned);
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

    // 같은 키의 씬 배치 몬스터 중 가장 가까운 것을 매칭 (거리 제한 없음, Items 패턴과 동일)
    private Monster FindPendingScenePlacedMonster(Monsters key, Vector3 pos)
    {
        Monster best = null;
        float bestDist = float.MaxValue;
        foreach (Monster m in pendingScenePlacedMonsters)
        {
            if (m == null) continue;
            if (m.MonsterKey != key) continue;

            float d = Vector3.Distance(m.transform.position, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = m;
            }
        }
        return best;
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
