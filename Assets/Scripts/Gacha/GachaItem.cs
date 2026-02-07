using UnityEngine;

[CreateAssetMenu(fileName = "New Gacha Item", menuName = "Gacha/Item")]
public class GachaItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public string rarity; // 예: "Common", "Rare", "Legendary"
    public int weight;    // 확률 가중치 (높을수록 잘 나옴)
}