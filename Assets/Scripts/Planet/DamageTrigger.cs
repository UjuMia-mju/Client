using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PeerPlayerStat tempStat = other.GetComponent<PeerPlayerStat>();
            tempStat.DecreaseHp(1);
            Debug.Log("트리거 : 데미지");
        }
    }
}