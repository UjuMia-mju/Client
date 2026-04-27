using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IngredientDictionary", menuName = "UjuMia/Ingredient Dictionary", order = 0)]
public class IngredientDictionaryData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Items.itemStringKey와 동일한 값")]
        public string key;
        public string displayName;
        public Sprite icon;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;
}
