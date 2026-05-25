using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제련 레시피와 결과 프리팹을 한 에셋에서 관리합니다.
/// SmeltingRecipeManager(레시피 조회)와 FurnaceClientManager(결과 프리팹 조회)가
/// 모두 같은 카탈로그를 참조하므로 양쪽 인스펙터를 동기화해야 하는 문제가 사라집니다.
/// </summary>
[CreateAssetMenu(fileName = "SmeltingCatalog", menuName = "UjuMia/Smelting Catalog")]
public class SmeltingCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("녹일 원재료 아이템 키 (ItemCatalog 키와 동일)")]
        [ItemKey] public string inputItemStringKey;

        [Tooltip("결과물 식별자 (FurnaceClientManager 결과 프리팹 매칭 키)")]
        public int outputItemID;

        [Tooltip("제련에 걸리는 시간(초)")]
        public float smeltingTime;

        [Tooltip("결과로 스폰할 프리팹")]
        public GameObject resultPrefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    private Dictionary<string, Entry> _byInputKey;
    private Dictionary<int, Entry> _byOutputId;

    private void EnsureCache()
    {
        if (_byInputKey != null && _byOutputId != null) return;

        _byInputKey = new Dictionary<string, Entry>(StringComparer.Ordinal);
        _byOutputId = new Dictionary<int, Entry>();
        foreach (Entry e in entries)
        {
            if (e == null) continue;
            if (!string.IsNullOrWhiteSpace(e.inputItemStringKey))
                _byInputKey[e.inputItemStringKey] = e;
            _byOutputId[e.outputItemID] = e;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => InvalidateCache();
#endif

    public void InvalidateCache()
    {
        _byInputKey = null;
        _byOutputId = null;
    }

    public bool TryGetByInputKey(string inputKey, out Entry entry)
    {
        EnsureCache();
        if (string.IsNullOrWhiteSpace(inputKey)) { entry = null; return false; }
        return _byInputKey.TryGetValue(inputKey, out entry);
    }

    public bool TryGetByOutputId(int outputId, out Entry entry)
    {
        EnsureCache();
        return _byOutputId.TryGetValue(outputId, out entry);
    }
}