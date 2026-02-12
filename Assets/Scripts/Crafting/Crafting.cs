using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class Crafting : MonoBehaviour
{
    private List<GameObject> craftItems = new List<GameObject>();

    private const float ITEM_THROW_HEIGHT = 3.5f; 
    private const float ITEM_THROW_FORCE = 200f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            Player player = other.GetComponent<Player>();
            player.Crafting(this);
            player.RemoveAllItemsFromCraftTable(this);
        }
    }

    public void AddCraftItems(GameObject data)
    {
        data.transform.SetParent(this.transform);

        // 비활성화된 요소들을 활성화
        Rigidbody rb = data.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        ObjectsGravityController objectGravityController = data.GetComponent<ObjectsGravityController>();
        objectGravityController.enabled = true;

        BoxCollider[] colliders = data.GetComponentsInChildren<BoxCollider>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }

        data.SetActive(false); // 조합대에 올려진 아이템은 월드 내에서 물리작용하지 않도록 비활성화
        craftItems.Add(data);

        // 리스트에 들어 있는 아이템 이름들을 문자열로 합치기
        string itemsText = string.Join(", ", craftItems.ConvertAll(item => item.name));

        // 출력합니다.
        // TODO : 추후 UI 담당하신 박지우님과 논의후 이것이 이미지로 표시가 가능해야 합니다. 현재는 디버그 로그로 대체합니다.
        Debug.Log("현재 조합대에 들어 있는 아이템들 : " + itemsText);
    }

    public void RemoveAllItems()
    {
        if (craftItems.Count == 0)
        {
            Debug.Log("조합대에 아이템이 없습니다.");
            return;
        }

        foreach (var listItem in craftItems)
        {
            listItem.SetActive(true); // 아이템을 다시 보이도록 활성화
            listItem.transform.SetParent(null); // 조합대의 자식에서 제거
            Rigidbody rb = listItem.GetComponent<Rigidbody>();
            listItem.transform.position = this.transform.position + this.transform.up * ITEM_THROW_HEIGHT;
            rb.AddForce((this.transform.up + this.transform.forward) * ITEM_THROW_FORCE);

            Debug.Log("현재 던진 아이템 이름 : " + listItem.name);
        }


        craftItems.Clear();
        Debug.Log("조합대의 모든 아이템이 제거되었습니다.");
    }   
}