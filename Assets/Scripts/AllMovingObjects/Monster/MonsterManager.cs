using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Protocol;

[DefaultExecutionOrder(-100)]
public class MonsterManager : MonoBehaviour
{
    [Header("몬스터 카탈로그 (키·프리팹 단일 관리)")]
    [SerializeField] private MonsterCatalog monsterCatalog;

    public MonsterCatalog Catalog => monsterCatalog;

    private Dictionary<int, GameObject> monsterDic = new Dictionary<int, GameObject>();

    // 피어가 호스트의 스폰 패킷을 받기 전까지 보관할 씬 배치 몬스터 목록
    private List<Monster> pendingScenePlacedMonsters = new List<Monster>();

    // 피어가 등록을 마치기 전에 호스트의 SpawnFromNetwork 가 먼저 도착한 경우를 위한 보류 큐
    private struct PendingSpawn
    {
        public Monsters key;
        public int id;
        public Vector3 pos;
        public Quaternion rot;
    }
    private readonly List<PendingSpawn> pendingNetworkSpawns = new List<PendingSpawn>();

    // 호스트: 씬 배치 몬스터 스폰을 Start 에서 일괄 브로드캐스트하기 위한 보관함
    private readonly List<Monster> pendingScenePlacedBroadcasts = new List<Monster>();

    // 등록(매칭) 절차가 완료됐는지. 피어의 "선도착 패킷" 버퍼링 종료 조건.
    private bool _scenePlacedRegistered;

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

        // ★ ID 부여만 Awake 에서 즉시 수행.
        //   [DefaultExecutionOrder(-100)] 덕분에 다른 Monster 의 Start 보다 먼저 실행되므로
        //   DesertWorm.Start 가 BroadcastAnimState 를 호출할 때 monsterId 가 이미 유효함.
        //   브로드캐스트(S_MONSTER_SPAWN) 자체는 Start 로 지연.
        Monster[] placed = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (Monster m in placed)
        {
            if (m == null) continue;
            if (!m.IsScenePlacedMonster) continue;
            PreRegisterScenePlacedMonster(m);
        }
    }

    private IEnumerator Start()
    {
        // ConnectManager.SetHostRole / PacketSender.Init 완료 보장용 1프레임 대기.
        yield return null;

        if (IsHost())
        {
            // 호스트: 미리 ID 부여해둔 씬 배치 몬스터들을 이 시점에 일괄 브로드캐스트.
            foreach (Monster m in pendingScenePlacedBroadcasts)
            {
                if (m == null) continue;
                BroadcastScenePlacedSpawn(m);
            }
        }
        pendingScenePlacedBroadcasts.Clear();

        _scenePlacedRegistered = true;

        // 피어: 등록 전에 먼저 도착해 있던 스폰 패킷 일괄 처리
        if (!IsHost() && pendingNetworkSpawns.Count > 0)
        {
            PendingSpawn[] buffered = pendingNetworkSpawns.ToArray();
            pendingNetworkSpawns.Clear();
            foreach (PendingSpawn s in buffered)
                SpawnFromNetwork(s.key, s.id, s.pos, s.rot);
        }
    }

    private static bool IsHost()
        => ConnectManager.Instance != null && ConnectManager.Instance.isHost;

    // Awake 단계 처리: 호스트는 ID 부여, 피어는 매칭 대기 큐에 적재.
    private void PreRegisterScenePlacedMonster(Monster monster)
    {
        if (monster == null) return;
        if (monster.MonsterKey == Monsters.None)
        {
            Debug.LogError($"[MonsterManager] 씬 배치 몬스터 '{monster.name}' 의 MonsterKey 가 None 입니다.", monster);
            return;
        }

        // ConnectManager 가 Awake 보다 먼저 SetHostRole 을 호출했다는 전제.
        // (스테이지 씬은 S_GAME_READY_TO_START / S_START_STAGE 처리 후 로드되므로 안전.)
        if (IsHost())
        {
            int newId = nextMonsterId++;
            monster.monsterId = newId;
            monsterDic[newId] = monster.gameObject;
            pendingScenePlacedBroadcasts.Add(monster);
        }
        else
        {
            pendingScenePlacedMonsters.Add(monster);
        }
    }

    private void BroadcastScenePlacedSpawn(Monster monster)
    {
        if (PacketSender.Instance == null)
        {
            Debug.LogWarning($"[MonsterManager] PacketSender 미초기화 - 씬 배치 몬스터 스폰 브로드캐스트 실패: id={monster.monsterId}");
            return;
        }

        Vector3 pos = monster.transform.position;
        Quaternion rot = monster.transform.rotation;
        S_MONSTER_SPAWN packet = new S_MONSTER_SPAWN
        {
            MonsterId = monster.monsterId,
            MonsterKey = (int)monster.MonsterKey,
            Pos = new PosInfo { X = pos.x, Y = pos.y, Z = pos.z },
            Rot = new RotInfo { X = rot.x, Y = rot.y, Z = rot.z, W = rot.w }
        };
        PacketSender.Instance.BroadcastMonsterSpawn(packet);
        Debug.Log($"[MonsterManager] 씬 배치 몬스터 동기화: id={monster.monsterId}, key={monster.MonsterKey}");
    }

    public void SpawnMonster(Monsters monsterKey, Vector3 pos, Quaternion rot)
    {
        if (!IsHost()) return;

        GameObject prefab = FindPrefabByKey(monsterKey);
        if (prefab == null) return;

        int newId = nextMonsterId++;
        GameObject spawned = Instantiate(prefab, pos, rot);
        AssignMonsterId(spawned, newId);
        monsterDic[newId] = spawned;

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
        if (!IsHost()) return;
        if (!monsterDic.ContainsKey(id)) return;

        GameObject spawned = monsterDic[id];
        monsterDic.Remove(id);

        if (spawned != null)
        {
            Monster monsterComp = spawned.GetComponent<Monster>();
            if (monsterComp != null) monsterComp.PlayDeathAndDestroy();
            else Destroy(spawned);
        }

        S_MONSTER_DEAD packet = new S_MONSTER_DEAD { MonsterId = id };
        PacketSender.Instance.BroadcastMonsterDead(packet);
    }

    public void SpawnFromNetwork(Monsters monsterKey, int id, Vector3 pos, Quaternion rot)
    {
        // 피어 측, MonsterManager.Start 가 아직 실행되지 않아 매칭 큐가 미구성된 시점이면 보류.
        if (!IsHost() && !_scenePlacedRegistered)
        {
            pendingNetworkSpawns.Add(new PendingSpawn { key = monsterKey, id = id, pos = pos, rot = rot });
            return;
        }

        Monster existing = FindPendingScenePlacedMonster(monsterKey, pos);
        if (existing != null)
        {
            existing.monsterId = id;
            monsterDic[id] = existing.gameObject;
            pendingScenePlacedMonsters.Remove(existing);

            existing.transform.SetPositionAndRotation(pos, rot);

            Debug.Log($"[MonsterManager] 씬 배치 몬스터 ID 동기화 (피어): key={monsterKey}, id={id}");
            return;
        }

        GameObject prefab = FindPrefabByKey(monsterKey);
        if (prefab == null) return;

        GameObject spawned = Instantiate(prefab, pos, rot);
        AssignMonsterId(spawned, id);
        monsterDic[id] = spawned;
    }

    public void DestroyFromNetwork(int id)
    {
        if (!monsterDic.ContainsKey(id)) return;

        GameObject spawned = monsterDic[id];
        monsterDic.Remove(id);

        if (spawned == null) return;

        Monster monsterComp = spawned.GetComponent<Monster>();
        if (monsterComp != null) monsterComp.PlayDeathAndDestroy();
        else Destroy(spawned);
    }

    public void UpdateAnimStateFromNetwork(int id, int stateInt)
    {
        if (!monsterDic.TryGetValue(id, out GameObject spawned) || spawned == null)
            return;

        DesertWormAnimator wormAnim = spawned.GetComponent<DesertWormAnimator>();
        if (wormAnim != null)
        {
            var state = (WormAnimState)stateInt;
            wormAnim.SetState(state);

            if (state == WormAnimState.TakeDamage)
            {
                var tint = spawned.GetComponent<DamageMeshTintController>();
                tint?.PlayHitFlash(1);
            }

            return;
        }
    }

    public void UpdateTransformFromNetwork(int id, Vector3 pos, Quaternion rot)
    {
        if (!monsterDic.TryGetValue(id, out GameObject spawned) || spawned == null)
            return;
        spawned.transform.SetPositionAndRotation(pos, rot);
    }

    private GameObject FindPrefabByKey(Monsters targetKey)
    {
        if (monsterCatalog == null)
        {
            Debug.LogError("[MonsterManager] MonsterCatalog 가 할당되지 않았습니다.");
            return null;
        }
        return monsterCatalog.GetPrefab(targetKey);
    }

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

    private void AssignMonsterId(GameObject spawned, int id)
    {
        if (spawned == null) return;

        DesertWorm worm = spawned.GetComponent<DesertWorm>();
        if (worm != null) { worm.monsterId = id; return; }
    }
}
