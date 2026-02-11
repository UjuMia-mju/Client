using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Crafting : MonoBehaviour
{
    private List<GameObject> craftItems = new List<GameObject>();

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            Player player = other.GetComponent<Player>();
            player.Crafting(this);
        }
    }

    public void AddCraftItems(GameObject data)
    {
        craftItems.Add(data);

        // 리스트에 들어 있는 아이템 이름들을 문자열로 합치기
        string itemsText = string.Join(", ", craftItems.ConvertAll(item => item.name));

        Debug.Log("현재 조합대에 들어 있는 아이템들 : " + itemsText);
    }
}