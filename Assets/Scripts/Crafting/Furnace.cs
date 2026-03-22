using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SmeltedItemData
{
    public string itemSmeltKey;
    public GameObject originalItemPrefab;
    public GameObject smeltedPrefab;
    public float smeltTime;
}

public class Furnace : MonoBehaviour
{
    // Inspector에서 프리팹들을 등록
    [SerializeField] private List<SmeltedItemData> smeltedItemList;

    private const float ITEM_THROW_HEIGHT = 3.5f;
    private const float ITEM_THROW_FORCE = 200f;
    private const float SMELTING_TIME = 1f;

    private float remainingTime;

    private string targetItemKey;
    private Coroutine smeltingCoroutine = null;

    // 플레이어가 아이템을 넣을 때 호출
    // 만약 조작을 했을 때 아이템을 넣을 상태가 아니라면, bool을 반환해 현재 상태를 알려야 플레이어가 자기가 아이템을 들고 있는지를 정확하게 판단할 수 있습니다.
    public bool AddSmeltTargetItem(GameObject data)
    {
        if (!data.CompareTag(Define.Tag.ITEM) || smeltingCoroutine != null)
        {
            Debug.Log("해당 객체가 아이템이 아니거나 용광로가 작동 중입니다.");
            return false;
        }

        Debug.Log("용광로실행");

        // 리스트에서 해당 아이템의 smeltTime 찾기
        // 리스트에서 해당 아이템의 smeltTime 찾기
        SmeltedItemData smeltedData = smeltedItemList.Find(
            item => item.originalItemPrefab != null
                 && item.itemSmeltKey == data.GetComponent<Items>().itemSmeltKey
        );


        if (smeltedData != null)
        {
            targetItemKey = smeltedData.itemSmeltKey;
            smeltingCoroutine = StartCoroutine(Smelt(smeltedData.smeltTime));
            Destroy(data); // 원본 아이템 제거
            return true;
        }

        else
        {
            Debug.Log("해당 아이템에 대한 SmeltedItemData가 없습니다: ");
            return false;
        }
    }


    private IEnumerator Smelt(float timerDuration)
    {
        remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            Debug.Log("남은 시간: " + remainingTime + "초");
            yield return new WaitForSeconds(SMELTING_TIME);
            remainingTime -= 1f;
        }
        ThrowSmeltedItem();
        smeltingCoroutine = null;
    }

    public void ThrowSmeltedItem()
    {
        if (targetItemKey == null)
        {
            return;
        }

        // targetItemName으로 대응되는 결과 프리팹 찾기
        SmeltedItemData smelted = smeltedItemList.Find(item =>
            item.originalItemPrefab != null && item.itemSmeltKey == targetItemKey);

        if (smelted == null)
        {
            return;
        }

        // 아이템 생성
        GameObject itemToThrow = Instantiate(smelted.smeltedPrefab);

        // Rigidbody 가져오기
        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        // 위치와 힘 적용
        itemToThrow.transform.position = this.transform.position + this.transform.up * ITEM_THROW_HEIGHT;
        rb.AddForce((this.transform.up + this.transform.forward) * ITEM_THROW_FORCE);

    }

}
