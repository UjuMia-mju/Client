using UnityEngine;

[CreateAssetMenu(fileName = "New Gacha Item", menuName = "Gacha/Item")]
public class GachaItem : ScriptableObject
{
    public string itemName;
    [TextArea(2, 5)] public string descriptionText;
    public Sprite icon;
    public ItemRarity rarity;
}