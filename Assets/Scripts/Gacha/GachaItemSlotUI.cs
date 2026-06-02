using UnityEngine;
using UnityEngine.UI;

public class GachaItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject commonFrame;
    [SerializeField] private GameObject rareFrame;
    [SerializeField] private GameObject epicFrame;
    [SerializeField] private GameObject legendaryFrame;

    private void Awake()
    {
        ResolveReferencesIfMissing();
    }

    public void SetItem(GachaItem item)
    {
        if (item == null)
            return;

        ResolveReferencesIfMissing();

        if (iconImage != null)
            iconImage.sprite = item.icon;

        ApplyRarityFrame(item.rarity);
    }

    private void ApplyRarityFrame(ItemRarity rarity)
    {
        if (commonFrame != null)
            commonFrame.SetActive(rarity == ItemRarity.Common);
        if (rareFrame != null)
            rareFrame.SetActive(rarity == ItemRarity.Rare);
        if (epicFrame != null)
            epicFrame.SetActive(rarity == ItemRarity.Epic);
        if (legendaryFrame != null)
            legendaryFrame.SetActive(rarity == ItemRarity.Legendary);
    }

    private void ResolveReferencesIfMissing()
    {
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (commonFrame == null)
            commonFrame = transform.Find("CommonFrame")?.gameObject;

        if (rareFrame == null)
            rareFrame = transform.Find("RareFrame")?.gameObject;

        if (epicFrame == null)
            epicFrame = transform.Find("EpicFrame")?.gameObject;

        if (legendaryFrame == null)
        {
            Transform legendary = transform.Find("LegendaryFrame");
            if (legendary == null)
                legendary = transform.Find("RegendaryFrame");
            legendaryFrame = legendary != null ? legendary.gameObject : null;
        }
    }
}
