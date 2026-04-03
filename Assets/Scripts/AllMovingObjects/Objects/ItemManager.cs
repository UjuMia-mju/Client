using Protocol;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<int, Items> itemDic = new Dictionary<int, Items>();

    // TODO : 현재는 씬 로드 시 Start() 호출 순서대로 ID를 부여하는 임시 방식입니다.
    // 추후 호스트가 아이템 ID를 부여하고 S_OBJECT_SPAWN 패킷으로 전체 클라이언트에 브로드캐스트하는 방식으로 변경 예정입니다.
    // 변경 시 이 필드는 제거하고 호스트로부터 받은 ID를 직접 사용하면 됩니다.
    private static int _nextItemId = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextItemId = 1; // 씬 로드 시 항상 1부터 초기화
    }

    public void RegisterItem(Items item)
    {
        item.itemId = _nextItemId++;
        if (!itemDic.ContainsKey(item.itemId))
        {
            itemDic.Add(item.itemId, item);
            Debug.Log($"✓ Registered item: {item.name} (id={item.itemId})");
        }
    }

    public void UnregisterItem(Items item)
    {
        if (itemDic.ContainsKey(item.itemId))
        {
            itemDic.Remove(item.itemId);
            Debug.Log($"✓ Unregistered item: {item.name} (id={item.itemId})");
        }
    }

    public Items GetItem(int id)
    {
        if (itemDic.TryGetValue(id, out Items item))
            return item;
        return null;
    }
}
