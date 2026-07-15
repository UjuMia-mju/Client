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

    [Header("인스펙터 수동 등록")]
    [Tooltip("씬에 배치된 Monster를 넣으면 리스트 순서대로 1,2,3... ID가 부여됩니다.\n호스트/피어 양쪽에서 같은 순서 = 같은 ID. 네트워크로 주고받지 않습니다.")]
    public List<Monster> inspectorScenePlacedMonsters = new List<Monster>();

    [Tooltip("인스펙터의 몬스터 항목들을 Awake 시 등록합니다.")]
    private bool populateMonsterDicFromInspector = true;

    // ── ID 대역 분리 ────────────────────────────────────────────────
    // 씬 배치 몬스터:  1 ~ (4000 미만)  → 리스트 순서대로 고정.
    // 동적 스폰 몬스터: 4000 이상        → nextMonsterId 로 부여.
    private const int DynamicIdBase = 4000;
    private int nextMonsterId = DynamicIdBase;

    // 피어가 등록을 마치기 전에 도착한 동적 스폰/파괴 보류 큐.
    private struct PendingSpawn
    {
        public Monsters key;
        public int id;
        public Vector3 pos;
        public Quaternion rot;
    }
    private readonly List<PendingSpawn> pendingNetworkSpawns = new List<PendingSpawn>();

    // 등록 절차 완료 여부. 피어의 "선도착 패킷" 버퍼링 종료 조건.
    private bool _scenePlacedRegistered;

    public static MonsterManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 동적 스폰 대역은 항상 4000부터 시작.
        nextMonsterId = DynamicIdBase;

        // 씬 배치 몬스터를 인스펙터 리스트 순서대로 1,2,3... 등록.
        // 씬 몬스터는 호스트/피어 양쪽에 이미 존재하므로 네트워크로 주고받지 않는다.
        // [DefaultExecutionOrder(-100)] 덕분에 다른 Monster.Start 보다 먼저 실행되어
        // monsterId 가 유효한 상태로 준비된다.
        if (populateMonsterDicFromInspector && inspectorScenePlacedMonsters != null)
        {
            int scenePlacedNextId = 1;
            foreach (Monster m in inspectorScenePlacedMonsters)
            {
                if (m == null) continue;
                RegisterScenePlacedMonster(m, ref scenePlacedNextId);
            }
        }
    }

    private IEnumerator Start()
    {
        // ConnectManager.SetHostRole / PacketSender.Init 완료 보장용 1프레임 대기.
        yield return null;

        _scenePlacedRegistered = true;

        // 피어: 등록 전에 먼저 도착해 있던 동적 스폰 패킷 일괄 처리.
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

    // ── 씬 배치 몬스터 전용 등록 ──────────────────────────────────────
    // 리스트 순서대로 1,2,3... 부여. 동적 대역(4000+)과 겹치지 않는다.
    // 호스트/피어 모두 같은 리스트를 쓰므로 같은 순서 = 같은 ID.
    private void RegisterScenePlacedMonster(Monster monster, ref int scenePlacedNextId)
    {
        if (monster == null) return;
        if (monster.MonsterKey == Monsters.None)
        {
            Debug.LogError($"[MonsterManager] 씬 배치 몬스터 '{monster.name}' 의 MonsterKey 가 None 입니다.", monster);
            return;
        }

        int newId = scenePlacedNextId++;
        monster.monsterId = newId;

        if (!monsterDic.ContainsKey(newId))
        {
            monsterDic[newId] = monster.gameObject;
            Debug.Log($"[MonsterManager] ✓ Registered scene monster: {monster.name} (id={newId}, key={monster.MonsterKey})");
        }
        else
        {
            Debug.LogWarning($"[MonsterManager] 씬 몬스터 id={newId} 중복. 대상: {monster.name}");
        }
    }

    // ── 호스트: 게임 중 동적 몬스터 스폰 ─────────────────────────────
    public void SpawnMonster(Monsters monsterKey, Vector3 pos, Quaternion rot)
    {
        Debug.Log("몬스터 소환");
        if (!IsHost())
        {
            Debug.Log("몬스터 : 호스트가 아니므로 동작하지 않음.");
            return; 
        } 

        GameObject prefab = FindPrefabByKey(monsterKey);
        if (prefab == null)
        {
            Debug.Log("몬스터: 몬스터 키가 없으므로 동작하지 않음.");
            return;
        }


        int newId = nextMonsterId++;   // 4000+ 대역

        Debug.Log($"몬스터 : Registered Monster = {newId}");

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

    // ── 피어: 네트워크 동적 스폰 수신 ────────────────────────────────
    // ★ 씬 배치 몬스터 거리 매칭 제거. 네트워크 스폰은 항상 새 프리팹을 Instantiate.
    //   씬 몬스터는 네트워크가 건드리지 않는다. (id 4000+ 는 씬 대역 1~ 과 겹치지 않음)
    public void SpawnFromNetwork(Monsters monsterKey, int id, Vector3 pos, Quaternion rot)
    {
        // 피어: 등록 완료 전 도착한 스폰은 보류.
        if (!IsHost() && !_scenePlacedRegistered)
        {
            pendingNetworkSpawns.Add(new PendingSpawn { key = monsterKey, id = id, pos = pos, rot = rot });
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

    // 애니메이션 수신부, 새 몬스터 추가 시 이곳에 확장할 것.
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

        KingSlimeAnimator slimeAnimator = spawned.GetComponent<KingSlimeAnimator>();
        if (slimeAnimator != null)
        {
            var state = (KingSlimeAnimState)stateInt;
            slimeAnimator.SetState(state);
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

    private void AssignMonsterId(GameObject spawned, int id)
    {
        if (spawned == null) return;

        DesertWorm worm = spawned.GetComponent<DesertWorm>();
        if (worm != null) { worm.monsterId = id; return; }
    }

    public void ApplyDamageFromNetwork(int id, int damage)
    {
        if (!monsterDic.TryGetValue(id, out GameObject spawned) || spawned == null)
            return;
        
        Monster monsterComp = spawned.GetComponent<Monster>();
        if (monsterComp != null) monsterComp.ApplyDamage(damage);
    }
}