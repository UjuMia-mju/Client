using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "IngredientDictionaryData",
    menuName = "HUD/Craft Bubble/Ingredient Dictionary Data")]
public class IngredientDictionaryData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("서버에서 내려오는 재료 키 (예: iron_ore, wood)")]
        public string key;
        public string displayName;
        public Sprite icon;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public List<Entry> MutableEntries => entries;
}
