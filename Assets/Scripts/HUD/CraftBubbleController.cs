using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftBubbleController : MonoBehaviour
{
    [Serializable]
    public class Ingredient
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI label;
    }

    [Header("Ingredients")]
    [SerializeField] private List<Ingredient> ingredientSlots = new List<Ingredient>();

    [Header("표시 이름·아이콘 소스")]
    [SerializeField] private ItemCatalog itemCatalog;
    [Tooltip("같은 key에 대해 카탈로그보다 우선합니다. displayName·icon이 비어 있지 않은 항목만 덮어씁니다. 카탈로그에 없는 key는 사전만으로 슬롯에 쓰입니다.")]
    [SerializeField] private IngredientDictionaryData ingredientDictionaryData;

    [SerializeField] private bool hideUnusedSlots = true;

    [Header("Spaceship Binding")]
    [SerializeField] private SpaceshipAssembly spaceshipAssembly;
    [SerializeField] private bool autoFindSpaceshipAssembly = true;
    [Tooltip("true면 라벨에 현재/목표 진행도를 표시합니다. (예: 철광석 x (1/3))")]
    [SerializeField] private bool showProgress = true;

    public IReadOnlyList<Ingredient> IngredientSlots => ingredientSlots;

    private readonly Dictionary<string, UIRow> _displayByKey = new Dictionary<string, UIRow>();

    /// <summary>아이콘·표시 문자열 캐시 (카탈로그 + 재료 사전 오버레이)</summary>
    private sealed class UIRow
    {
        public Sprite Icon;
        public string DisplayName;
    }

    private void Awake()
    {
        RebuildIngredientDictionary();
    }

    private void OnEnable()
    {
        if (autoFindSpaceshipAssembly && spaceshipAssembly == null)
            spaceshipAssembly = FindFirstObjectByType<SpaceshipAssembly>();

        ApplySpaceshipRequirements();
    }

    private void Update()
    {
        if (spaceshipAssembly == null)
            return;
        ApplySpaceshipRequirements();
    }

    /// <summary>
    /// SpaceshipAssembly의 targetMission(current/target)을 그대로 읽어 버블 UI를 갱신합니다.
    /// </summary>
    public void ApplySpaceshipRequirements()
    {
        if (spaceshipAssembly == null)
        {
            // 참조가 없으면 슬롯 정리만 수행
            for (int i = 0; i < ingredientSlots.Count; i++)
                BindEmptySlot(ingredientSlots[i]);
            return;
        }

        IReadOnlyList<SpaceshipMission> missions = spaceshipAssembly.TargetMission;
        int missionCount = missions != null ? missions.Count : 0;

        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            if (missions == null || i >= missionCount)
            {
                BindEmptySlot(ingredientSlots[i]);
                continue;
            }

            BindMission(ingredientSlots[i], missions[i]);
        }

    }

    public void SetSpaceshipAssembly(SpaceshipAssembly assembly)
    {
        spaceshipAssembly = assembly;
        ApplySpaceshipRequirements();
    }

    private void BindMission(Ingredient slot, SpaceshipMission mission)
    {
        SetSlotVisible(slot, true);
        if (slot == null)
            return;

        string key = mission != null && mission.targetItem != null
            ? mission.targetItem.itemStringKey
            : string.Empty;
        int current = mission != null ? Mathf.Max(0, mission.currentCount) : 0;
        int target = mission != null ? Mathf.Max(0, mission.targetCount) : 0;

        if (_displayByKey.TryGetValue(key, out UIRow info))
        {
            if (slot.icon != null)
            {
                slot.icon.sprite = info.Icon;
                slot.icon.enabled = info.Icon != null;
            }

            if (slot.label != null)
            {
                string display = string.IsNullOrWhiteSpace(info.DisplayName) ? key : info.DisplayName;
                slot.label.text = showProgress ? $"{display} ({current}/{target})" : $"{display} x ({target})";
            }
            return;
        }

        if (slot.icon != null)
        {
            slot.icon.sprite = null;
            slot.icon.enabled = false;
        }

        if (slot.label != null)
            slot.label.text = showProgress ? $"{key} ({current}/{target})" : $"{key} x ({target})";
    }

    private void BindEmptySlot(Ingredient slot)
    {
        if (hideUnusedSlots)
        {
            SetSlotVisible(slot, false);
            return;
        }

        SetSlotVisible(slot, true);
        if (slot == null)
            return;

        if (slot.icon != null)
        {
            slot.icon.sprite = null;
            slot.icon.enabled = false;
        }

        if (slot.label != null)
            slot.label.text = string.Empty;
    }

    private void SetSlotVisible(Ingredient slot, bool visible)
    {
        if (slot == null)
            return;

        if (slot.root != null)
        {
            slot.root.SetActive(visible);
            return;
        }

        if (slot.icon != null)
            slot.icon.gameObject.SetActive(visible);
        if (slot.label != null)
            slot.label.gameObject.SetActive(visible);
    }

    private void RebuildIngredientDictionary()
    {
        _displayByKey.Clear();

        if (itemCatalog != null)
        {
            IReadOnlyList<ItemCatalog.Entry> catalogEntries = itemCatalog.Entries;
            for (int i = 0; i < catalogEntries.Count; i++)
            {
                ItemCatalog.Entry e = catalogEntries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.key))
                    continue;

                _displayByKey[e.key] = new UIRow { Icon = e.icon, DisplayName = e.displayName };
            }
        }

        if (ingredientDictionaryData == null)
            return;

        IReadOnlyList<IngredientDictionaryData.Entry> dictionaryEntries = ingredientDictionaryData.Entries;
        for (int i = 0; i < dictionaryEntries.Count; i++)
        {
            IngredientDictionaryData.Entry entry = dictionaryEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            if (_displayByKey.TryGetValue(entry.key, out UIRow row))
            {
                if (!string.IsNullOrWhiteSpace(entry.displayName))
                    row.DisplayName = entry.displayName;
                if (entry.icon != null)
                    row.Icon = entry.icon;
            }
            else
            {
                _displayByKey[entry.key] = new UIRow { Icon = entry.icon, DisplayName = entry.displayName };
            }
        }
    }

}
