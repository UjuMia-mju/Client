using Protocol;
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

    [Header("Actions")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        Hide();
        BindCloseButton();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickConfirm);
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.AddListener(OnClickConfirm);
    }

    public void Show(GachaItem item, SkinInfo serverSkin = null)
    {
        if (item == null)
            return;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (iconImage != null)
            iconImage.sprite = item.icon;

        if (nameText != null)
        {
            string name = string.IsNullOrWhiteSpace(item.itemName) ? "-" : item.itemName.Trim();
            nameText.text = $"< {name} >";
        }

        ApplyRarityText(serverSkin, item);

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(item.descriptionText) ? "-" : item.descriptionText;
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void OnClickConfirm()
    {
        SoundManager.Instance.PlaySFX("Click2");
        Hide();
    }

    private void ApplyRarityText(SkinInfo serverSkin, GachaItem item)
    {
        if (rarityText == null)
            return;

        if (serverSkin != null && TryGetRarityFromServer(serverSkin.Rarity, out ItemRarity rarity, out string displayName))
        {
            rarityText.text = displayName;
            rarityText.color = GetRarityColor(rarity);
            return;
        }

        rarityText.text = item.rarity.ToString();
        rarityText.color = GetRarityColor(item.rarity);
    }

    /// <summary>서버 rarity: 1=일반, 2=레어, 3=에픽, 4=레전더리</summary>
    private static bool TryGetRarityFromServer(int serverRarity, out ItemRarity rarity, out string displayName)
    {
        switch (serverRarity)
        {
            case 1:
                rarity = ItemRarity.Common;
                displayName = "일반";
                return true;
            case 2:
                rarity = ItemRarity.Rare;
                displayName = "레어";
                return true;
            case 3:
                rarity = ItemRarity.Epic;
                displayName = "에픽";
                return true;
            case 4:
                rarity = ItemRarity.Legendary;
                displayName = "레전더리";
                return true;
            default:
                rarity = ItemRarity.Common;
                displayName = string.Empty;
                return false;
        }
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return Color.magenta; // Unity purple 계열
            case ItemRarity.Legendary:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
}
