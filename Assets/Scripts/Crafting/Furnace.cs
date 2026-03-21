using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VersionControl;
using UnityEngine;


[System.Serializable]
public class SmeltedItemData
{
    public GameObject originalItemPrefab;
    public GameObject smeltedPrefab;
    public float smeltTime;
}


public class Furnace : MonoBehaviour
{
    private string targetItemName; // 용광로에 굽는 아이템의 이름   

    // Inspector에서 프리팹들을 등록
    [SerializeField] private List<SmeltedItemData> smeltedItemList;

    private const float ITEM_THROW_HEIGHT = 3.5f;
    private const float ITEM_THROW_FORCE = 200f;
    private const float SMELTING_TIME = 1f;

    private float remainingTime;

    // 플레이어가 아이템을 넣을 때 호출
    public void AddSmeltTargetItem(GameObject data)
    {
        if (!data.CompareTag(Define.Tag.ITEM))
            return;

        targetItemName = data.name; // 이름 저장

        // 리스트에서 해당 아이템의 smeltTime 찾기
        SmeltedItemData smeltedData = smeltedItemList.Find(item => item.originalItemPrefab != null && item.originalItemPrefab.name == targetItemName);

        if (smeltedData != null)
        {
            // 코루틴 시작
            StartCoroutine(Smelt(smeltedData.smeltTime));
        }
        else
        {
            Debug.Log("해당 아이템에 대한 SmeltedItemData가 없습니다: " + targetItemName);
        }

        Destroy(data); // 원본 아이템 제거
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
    }

    public void ThrowSmeltedItem()
    {
        if (targetItemName == null)
        {
            Debug.Log("targetItem이 설정되지 않았습니다.");
            return;
        }

        // targetItemName으로 대응되는 결과 프리팹 찾기
        SmeltedItemData smelted = smeltedItemList.Find(item =>
            item.originalItemPrefab != null && item.originalItemPrefab.name == targetItemName);

        if (smelted == null)
        {
            Debug.Log("결과 프리팹을 찾을 수 없습니다: " + targetItemName);
            return;
        }

        // 아이템 생성
        GameObject itemToThrow = Instantiate(smelted.smeltedPrefab);

        // Rigidbody 가져오기
        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning(itemToThrow.name + "에 Rigidbody가 없습니다.");
            return;
        }

        // 위치와 힘 적용
        itemToThrow.transform.position = this.transform.position + this.transform.up * ITEM_THROW_HEIGHT;
        rb.AddForce((this.transform.up + this.transform.forward) * ITEM_THROW_FORCE);

        Debug.Log("던진 아이템 이름 : " + itemToThrow.name);

    }

}
