using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER) && other.GetComponent<Player>())
        {
            PlayerStat tempStat = other.GetComponent<PlayerStat>();
            tempStat.DecreaseHp(1);
        }
    }
}