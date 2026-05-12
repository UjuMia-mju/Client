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
        SoundManager.Instance.PlaySFX("Click2");
        Hide();
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return Color.purple;
            case ItemRarity.Legendary:
                return Color.yellow;
            // 만약 후에 신화 등급과 같은 새로운 등급을 추가해야 한다면
            // case ItemRarity.Mythic:
            //     return new Color.Red;
            default:
                return Color.white;
        }
    }
}
