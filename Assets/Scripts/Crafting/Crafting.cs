using System.Collections.Generic;
using UnityEngine;


// NOTE : 이 클래스는 조합대에 아이템을 올리고 제거하는 기능을 담당합니다.
// 20260310 기능추가 : 이제 프리팹을 인스펙터에서 등록하고, Awake에서 딕셔너리로 변환해 패킷을 받을 때 이 딕셔너리를 참조해 craftItems 리스트를 초기화해줍니다.
// 
public class Crafting : MonoBehaviour
{
    
    // Inspector에서 프리팹들을 등록
    [SerializeField] private List<GameObject> itemPrefabList;
    // 바로 윗줄 리스트를 Awake에서 딕셔너리로 전환
    private Dictionary<string, GameObject> itemPrefabDict = new Dictionary<string, GameObject>();

    private List<GameObject> craftItems = new List<GameObject>();

    private const float ITEM_THROW_HEIGHT = 3.5f; 
    private const float ITEM_THROW_FORCE = 200f;

    private int lastItemCount = 0;

    private void Awake()
    {
        foreach (var prefab in itemPrefabList)
        {
            if (prefab != null && !itemPrefabDict.ContainsKey(prefab.name))
            {
                itemPrefabDict[prefab.name] = prefab;
            }
        }
    }

    private void LateUpdate()
    {
        SendItemListToServer();
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
    
    // 이름으로 프리팹 가져오기
    public GameObject GetPrefab(string itemName)
    {
        if (itemPrefabDict.TryGetValue(itemName, out GameObject prefab))
        {
            return prefab;
        }
        Debug.LogWarning($"프리팹 {itemName}을 찾을 수 없습니다.");
        return null;
    }

    private void SendItemListToServer()
    {
        int itemCount = craftItems.Count;

        if (itemCount != lastItemCount)
        {
            NetManager.Instance.SendCraftingList(craftItems.ConvertAll(item => item.name));
            lastItemCount = itemCount;
        }
    }

    public void SetItemList(List<string> data)
    {
        foreach (string item in data)
        {
            // 프리팹 매핑 딕셔너리에서 찾아서 인스턴스 생성
            if (itemPrefabDict.TryGetValue(item, out GameObject prefab))
            {
                GameObject newItem = Instantiate(prefab);
                newItem.transform.SetParent(this.transform);
                newItem.SetActive(false);
                craftItems.Add(newItem);
            }
            else
            {
                Debug.Log("딕셔너리에 없는 아이템입니다.");
            }
        }
    }
}