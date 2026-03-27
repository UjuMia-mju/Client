using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Item UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text descriptionText;

    private void Awake()
    {
        Hide();
    }

    public void Show(GachaItem item)
    {
        if (item == null)
        {
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (iconImage != null)
            iconImage.sprite = item.icon;

        if (nameText != null)
            nameText.text = item.itemName;

        if (rarityText != null)
        {
            rarityText.text = item.rarity.ToString();
            rarityText.color = GetRarityColor(item.rarity);
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(item.descriptionText) ? "-" : item.descriptionText;
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    // UI 버튼 OnClick에 연결해서 결과 팝업을 닫을 때 사용
    public void OnClickConfirm()
    {
        Hide();
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rare:
                return new Color(0.35f, 0.70f, 1.00f);
            case ItemRarity.Epic:
                return new Color(0.75f, 0.45f, 1.00f);
            case ItemRarity.Legendary:
                return new Color(1.00f, 0.70f, 0.20f);
            default:
                return Color.white;
        }
    }
}
