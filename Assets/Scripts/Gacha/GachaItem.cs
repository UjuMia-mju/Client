using UnityEngine;

[CreateAssetMenu(fileName = "New Gacha Item", menuName = "Gacha/Item")]
public class GachaItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemRarity rarity;
    public int weight;    // 확률 가중치 (높을수록 잘 나옴) 모든 아이템의 가중치 합이 100이 될 필요는 없음.
}