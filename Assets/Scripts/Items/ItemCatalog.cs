using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 키·표시 이름·아이콘·프리팹을 한 에셋에서 관리합니다.
/// ItemManager 스폰용과 CraftBubble 표시 모두 같은 목록을 씁니다.
/// </summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "UjuMia/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Items.itemStringKey·패킷·미션 등과 동일한 식별자")]
        public string key;

        [Tooltip("UI 표시 이름. 비어 있으면 key를 그대로 씁니다.")]
        public string displayName;

        public Sprite icon;

        [Tooltip("스폰이 필요 없는 재료(미션 키만)는 비워 둘 수 있습니다.")]
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    private Dictionary<string, Entry> _byKey;
    private Dictionary<string, Entry> _byPrefabName; // [추가] 역방향 조회용

    private void EnsureCache()
    {
        if (_byKey != null)
            return;

        _byKey = new Dictionary<string, Entry>(StringComparer.Ordinal);
        _byPrefabName = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (Entry e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.key))
                continue;

            _byKey[e.key] = e;

            if (e.prefab != null)
                _byPrefabName[e.prefab.name] = e;
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
        _byPrefabName = null;
    }

    public bool TryGet(string key, out Entry entry)
    {
        EnsureCache();
        if (string.IsNullOrWhiteSpace(key))
        {
            entry = null;
            return false;
        }

        return _byKey.TryGetValue(key, out entry);
    }

    /// <returns>등록되어 있으면 해당 프리팹, 없거나 비어 있으면 null</returns>
    public GameObject GetPrefab(string key)
    {
        return TryGet(key, out Entry e) ? e.prefab : null;
    }

    /// <summary>
    /// [추가] 인스턴스 GameObject로부터 카탈로그 키를 역으로 찾는다.
    /// "Iron_Ore(Clone)", "Iron_Ore (1)" 등의 접미사를 제거 후 프리팹 이름과 매칭.
    /// 매칭 실패 시 null. (호출 측에서 에러 처리)
    /// </summary>
    public string ResolveKey(GameObject instance)
    {
        if (instance == null) return null;

        string raw = instance.name;

        // (Clone) 제거
        int cloneIdx = raw.IndexOf("(Clone)", StringComparison.Ordinal);
        if (cloneIdx >= 0) raw = raw.Substring(0, cloneIdx);

        // " (1)" 같은 중복 인스턴스 접미사 제거
        int parenIdx = raw.LastIndexOf(" (", StringComparison.Ordinal);
        if (parenIdx > 0 && raw.EndsWith(")", StringComparison.Ordinal))
            raw = raw.Substring(0, parenIdx);

        raw = raw.Trim();

        EnsureCache();
        return _byPrefabName.TryGetValue(raw, out Entry e) ? e.key : null;
    }
}
