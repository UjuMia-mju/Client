using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 키·프리팹을 한 에셋에서 관리합니다.
/// MonsterManager 가 씬을 옮겨도 같은 카탈로그를 참조하도록 ScriptableObject 화.
/// </summary>
[CreateAssetMenu(fileName = "MonsterCatalog", menuName = "UjuMia/Monster Catalog")]
public class MonsterCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Monsters enum 키")]
        public Monsters key;

        [Tooltip("스폰에 사용할 프리팹")]
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    private Dictionary<Monsters, Entry> _byKey;

    private void EnsureCache()
    {
        if (_byKey != null) return;

        _byKey = new Dictionary<Monsters, Entry>();
        foreach (Entry e in entries)
        {
            if (e == null || e.key == Monsters.None) continue;
            _byKey[e.key] = e;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InvalidateCache();
    }
#endif

    public void InvalidateCache()
    {
        _byKey = null;
    }

    public bool TryGet(Monsters key, out Entry entry)
    {
        EnsureCache();
        if (key == Monsters.None)
        {
            entry = null;
            return false;
        }
        return _byKey.TryGetValue(key, out entry);
    }

    public GameObject GetPrefab(Monsters key)
    {
        return TryGet(key, out Entry e) ? e.prefab : null;
    }
}