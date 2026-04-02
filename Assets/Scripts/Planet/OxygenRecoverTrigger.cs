using UnityEngine;

public class OxygenRecoverTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER) && other.GetComponent<Player>())
        {
            PlayerStat tempStat = other.GetComponent<PlayerStat>();
            tempStat.StartOxygenRecovery();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER) && other.GetComponent<Player>())
        {
            PlayerStat tempStat = other.GetComponent<PlayerStat>();
            tempStat.StopOxygenRecovery();
        }
    }
}