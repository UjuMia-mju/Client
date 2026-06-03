using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// stringKey → 표시 문구를 한 에셋에서 관리합니다.
/// <c>{0}</c>, <c>{1}</c> … 형식 인자를 지원합니다.
/// </summary>
[CreateAssetMenu(fileName = "MessageCatalog", menuName = "UjuMia/Message Catalog")]
public class MessageCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("MessageKeys 와 동일한 식별자")]
        public string key;

        [TextArea(1, 4)]
        public string text;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, string> _byKey;

    public IReadOnlyList<Entry> Entries => entries;

    private void EnsureCache()
    {
        if (_byKey != null) return;

        _byKey = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Entry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
            _byKey[entry.key.Trim()] = entry.text ?? string.Empty;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _byKey = null;
    }
#endif

    public bool TryGet(string key, out string text)
    {
        EnsureCache();
        if (string.IsNullOrWhiteSpace(key))
        {
            text = null;
            return false;
        }

        return _byKey.TryGetValue(key.Trim(), out text);
    }

    public string Get(string key)
    {
        if (TryGet(key, out string text))
            return text;

        Debug.LogWarning($"[MessageCatalog] 알 수 없는 key: {key}");
        return key;
    }

    public string Format(string key, params object[] args)
    {
        string template = Get(key);
        if (args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException e)
        {
            Debug.LogWarning($"[MessageCatalog] Format 실패 key={key}: {e.Message}");
            return template;
        }
    }
}
