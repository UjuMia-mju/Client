using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자원 키·프리팹을 한 에셋에서 관리합니다.
/// ResourceManager 가 씬을 옮겨도 같은 카탈로그를 참조하도록 ScriptableObject 화.
/// </summary>
[CreateAssetMenu(fileName = "ResourceCatalog", menuName = "UjuMia/Resource Catalog")]
public class ResourceCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("ResourceObject.resourceStringKey 와 동일한 식별자")]
        public string key;

        [Tooltip("스폰에 사용할 프리팹")]
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    private Dictionary<string, Entry> _byKey;

    private void EnsureCache()
    {
        if (_byKey != null) return;

        _byKey = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (Entry e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.key)) continue;
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

    public GameObject GetPrefab(string key)
    {
        return TryGet(key, out Entry e) ? e.prefab : null;
    }
}