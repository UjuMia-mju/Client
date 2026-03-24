using System.Collections.Generic;
using UnityEditorInternal.VersionControl;
using UnityEngine;

public class Ore : MonoBehaviour
{

    // Inspector에서 프리팹 등록
    [SerializeField] private GameObject orePrefab;

    private int miningCount = 0;

    private const float ORE_THROW_HEIGHT = 3.5f;
    private const float ORE_THROW_FORCE = 150f;

    public void Mine()
    {
        Debug.Log("채굴");
        miningCount++;
        if (miningCount >= 3) // 3회 이상 채굴 시 아이템 드롭
        {
            DropItem();
            miningCount = 0; // 채굴 횟수 초기화
        }
    }

    private void DropItem()
    {
        Debug.Log("채굴 아이템 드랍");

        // 새로 생성된 오브젝트를 변수에 담기
        GameObject ore = Instantiate(orePrefab);

        // 위치 설정
        ore.transform.position = this.transform.position + this.transform.up * ORE_THROW_HEIGHT;

        // Rigidbody 가져와서 힘 주기
        Rigidbody rb = ore.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce((this.transform.up + this.transform.forward) * ORE_THROW_FORCE);
        }

    }
}
