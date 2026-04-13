using UnityEngine;

[System.Serializable]
public struct SmeltingRecipe
{
    public string inputItemStringKey; // 추가
    public int outputItemID;  // 용광로에서 나오는 아이템의 ID
    public float smeltingTime; // 용광로에서 아이템이 완성되기까지 걸리는 시간
}
